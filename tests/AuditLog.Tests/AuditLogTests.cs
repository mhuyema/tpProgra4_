using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuditLog.Api.Data;
using AuditLog.Api.Handlers;
using AuditLog.Api.Models;
using Broker.Messaging.Contracts;
using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AuditLog.Tests;

public class AuditLogTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrEmptyIdentityGetsGeneratedAndOtherFieldsRemainNull(bool empty)
    {
        using var factory = new AuditLogFactory();
        var before = DateTime.UtcNow.AddSeconds(-1);
        await factory.Receive(new AuditLogMessage(empty ? Guid.Empty : null, empty ? default(DateTime) : null));
        var logs = await factory.CreateClient().GetFromJsonAsync<AuditLogEntry[]>("/logs");
        var log = Assert.Single(logs!);
        Assert.NotEqual(Guid.Empty, log.LogId);
        Assert.InRange(log.DateUtc, before, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(DateTimeKind.Utc, log.DateUtc.Kind);
        Assert.Null(log.CorrelationId);
        Assert.Null(log.Service);
        Assert.Null(log.Action);
        Assert.Null(log.Message);
        Assert.Null(log.Details);
    }

    [Fact]
    public async Task ProvidedValuesAndNestedDetailsSurviveAndRedeliveryDoesNotDuplicate()
    {
        using var factory = new AuditLogFactory();
        var id = Guid.NewGuid();
        var date = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
        var details = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>("""{"nested":{"items":[1,true,null,"value"]},"empty":""}""");
        var message = new AuditLogMessage(id, date, "", "", "", "", details);
        await factory.Receive(message);
        await factory.Receive(message);
        var log = Assert.Single((await factory.CreateClient().GetFromJsonAsync<AuditLogEntry[]>("/logs"))!);
        Assert.Equal(id, log.LogId);
        Assert.Equal(date, log.DateUtc);
        Assert.Equal("", log.Service);
        Assert.Equal("", log.Action);
        Assert.Equal("", log.Message);
        Assert.Equal("", log.CorrelationId);
        Assert.Equal("value", log.Details!.Value.GetProperty("nested").GetProperty("items")[3].GetString());
    }

    [Fact]
    public async Task PagesAreLimitedTo50WithStableOrderEvenForEqualDates()
    {
        using var factory = new AuditLogFactory();
        var date = DateTime.UtcNow;
        for (var i = 0; i < 105; i++)
            await factory.Receive(new AuditLogMessage(Guid.NewGuid(), date));
        var client = factory.CreateClient();
        var first = (await client.GetFromJsonAsync<AuditLogEntry[]>("/logs"))!;
        var second = (await client.GetFromJsonAsync<AuditLogEntry[]>("/logs?page=2"))!;
        var third = (await client.GetFromJsonAsync<AuditLogEntry[]>("/logs?page=3"))!;
        Assert.Equal(50, first.Length);
        Assert.Equal(50, second.Length);
        Assert.Equal(5, third.Length);
        Assert.Equal(105, first.Concat(second).Concat(third).Select(x => x.LogId).Distinct().Count());
        Assert.Empty((await client.GetFromJsonAsync<AuditLogEntry[]>("/logs?page=4"))!);
    }

    [Fact]
    public async Task FiltersCombineAndDateBoundariesAreInclusive()
    {
        using var factory = new AuditLogFactory();
        var id = Guid.NewGuid();
        var date = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
        await factory.Receive(new AuditLogMessage(id, date, "Booking", "Created", CorrelationId: "request-1"));
        await factory.Receive(new AuditLogMessage(Guid.NewGuid(), date.AddDays(-1), "Booking", "Created", CorrelationId: "request-1"));
        await factory.Receive(new AuditLogMessage(Guid.NewGuid(), date, "Other", "Created", CorrelationId: "request-1"));
        var client = factory.CreateClient();
        foreach (var name in new[] { "correletionId", "correlationId" })
        {
            var logs = await client.GetFromJsonAsync<AuditLogEntry[]>(
                $"/logs?{name}=request-1&action=Created&service=Booking&logId={id}&startDate=2026-10-31T12:00:00Z&endDate=2026-10-31T12:00:00Z");
            Assert.Equal(id, Assert.Single(logs!).LogId);
        }
        Assert.Empty((await client.GetFromJsonAsync<AuditLogEntry[]>("/logs?action=Missing"))!);
        Assert.Single((await client.GetFromJsonAsync<AuditLogEntry[]>("/logs?service=Booking&startDate=2026-10-31T12:00:00Z&endDate=2026-10-31T12:00:00Z"))!);
        Assert.Equal(2, (await client.GetFromJsonAsync<AuditLogEntry[]>("/logs?service=Booking"))!.Length);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=2147483647")]
    [InlineData("logId=invalid")]
    [InlineData("startDate=invalid")]
    [InlineData("startDate=2026-11-01&endDate=2026-10-31")]
    public async Task InvalidQueryReturnsBadRequest(string query)
    {
        using var factory = new AuditLogFactory();
        var response = await factory.CreateClient().GetAsync($"/logs?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LogsPersistAfterDatabaseIsReopened()
    {
        var path = Path.Combine(Path.GetTempPath(), $"auditlog-test-{Guid.NewGuid():N}.db");
        var id = Guid.NewGuid();
        try
        {
            using (var db = new LiteDatabase(path) { UtcDate = true })
                await new AuditLogMessageHandler(new AuditLogRepository(db)).HandleAsync(
                    new AuditLogMessage(id, Service: "Booking"), "", CancellationToken.None);
            using var reopened = new LiteDatabase(path) { UtcDate = true };
            Assert.Equal(id, Assert.Single(new AuditLogRepository(reopened).Find(new LogQuery())).LogId);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

internal sealed class AuditLogFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // La prueba ejercita HTTP, handler y LiteDB reales sin conexión al broker remoto.
            services.RemoveAll<IHostedService>();
            services.RemoveAll<ILiteDatabase>();
            services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(new MemoryStream()) { UtcDate = true });
        });
    }

    public async Task Receive(AuditLogMessage message)
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuditLogMessageHandler>()
            .HandleAsync(message, "broker-correlation", CancellationToken.None);
    }
}

