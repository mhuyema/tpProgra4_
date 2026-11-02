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
        var logs = await (await factory.LoginClient()).GetFromJsonAsync<AuditLogEntry[]>("/logs");
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
        var log = Assert.Single((await (await factory.LoginClient()).GetFromJsonAsync<AuditLogEntry[]>("/logs"))!);
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
        var client = await factory.LoginClient();
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
        var client = await factory.LoginClient();
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
    [InlineData("sortBy=unknown")]
    [InlineData("sortDirection=unknown")]
    public async Task InvalidQueryReturnsBadRequest(string query)
    {
        using var factory = new AuditLogFactory();
        var response = await (await factory.LoginClient()).GetAsync($"/logs?{query}");
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

    [Fact]
    public async Task LoginProtectsLogsAndLogoutEndsTheSession()
    {
        using var factory = new AuditLogFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/session", new { password = "wrong" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/logs")).StatusCode);
        var login = await client.PostAsJsonAsync("/session", new { password = "test-password" });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/session")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/logs")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/logs")).StatusCode);
    }

    [Fact]
    public async Task MissingPasswordDoesNotAllowLogin()
    {
        using var factory = new AuditLogFactory();
        using var missingPassword = factory.WithWebHostBuilder(builder => builder.UseSetting("AuditLog:Password", ""));
        var response = await missingPassword.CreateClient().PostAsJsonAsync("/session", new { password = "" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("dateUtc", "asc")]
    [InlineData("dateUtc", "desc")]
    [InlineData("service", "asc")]
    [InlineData("service", "desc")]
    [InlineData("action", "asc")]
    [InlineData("message", "desc")]
    [InlineData("correlationId", "asc")]
    [InlineData("logId", "desc")]
    public async Task SortingAppliesBeforePagination(string sortBy, string direction)
    {
        using var factory = new AuditLogFactory();
        var entries = Enumerable.Range(0, 55).Select(i => new AuditLogMessage(
            Guid.NewGuid(), DateTime.UtcNow.Date.AddMinutes(i), $"Service-{i:D2}", $"Action-{i:D2}",
            $"Message-{i:D2}", $"Correlation-{i:D2}")).ToArray();
        foreach (var entry in entries.Reverse()) await factory.Receive(entry);
        var client = await factory.LoginClient();
        var query = $"/logs?sortBy={sortBy}&sortDirection={direction}";
        var first = (await client.GetFromJsonAsync<AuditLogEntry[]>(query))!;
        var second = (await client.GetFromJsonAsync<AuditLogEntry[]>(query + "&page=2"))!;
        Func<AuditLogMessage, object?> key = sortBy switch
        {
            "service" => x => x.Service,
            "action" => x => x.Action,
            "message" => x => x.Message,
            "correlationId" => x => x.CorrelationId,
            "logId" => x => x.LogId,
            _ => x => x.DateUtc
        };
        var expected = direction == "asc" ? entries.OrderBy(key) : entries.OrderByDescending(key);
        Assert.Equal(50, first.Length);
        Assert.Equal(5, second.Length);
        Assert.Equal(expected.Select(x => x.LogId), first.Concat(second).Select(x => (Guid?)x.LogId));
    }

    [Fact]
    public async Task LoginPageAndAssetsArePublic()
    {
        using var factory = new AuditLogFactory();
        var client = factory.CreateClient();
        Assert.Contains("Iniciar sesión", await client.GetStringAsync("/"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/app.js")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/styles.css")).StatusCode);
    }
}

internal sealed class AuditLogFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("AuditLog:Password", "test-password");
        builder.ConfigureServices(services =>
        {
            // La prueba ejercita HTTP, handler y LiteDB reales sin conexión al broker remoto.
            services.RemoveAll<IHostedService>();
            services.RemoveAll<ILiteDatabase>();
            services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(new MemoryStream()) { UtcDate = true });
        });
    }

    public async Task<HttpClient> LoginClient()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/session", new { password = "test-password" });
        response.EnsureSuccessStatusCode();
        return client;
    }

    public async Task Receive(AuditLogMessage message)
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuditLogMessageHandler>()
            .HandleAsync(message, "broker-correlation", CancellationToken.None);
    }
}
