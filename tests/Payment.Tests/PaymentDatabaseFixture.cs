using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Payment.Data;
using Xunit;

namespace Payment.Tests;

[CollectionDefinition(Name)]
public sealed class PaymentDatabaseCollection : ICollectionFixture<PaymentDatabaseFixture>
{
    public const string Name = "Payment SQL Server";
}

public sealed class PaymentDatabaseFixture : IAsyncLifetime
{
    private readonly string? _localDbInstanceName;
    private bool _localDbInstanceCreated;

    public string ConnectionString { get; }

    public PaymentDatabaseFixture()
    {
        var testDatabaseName = $"PaymentTests_{Guid.NewGuid():N}";
        var configuredConnection = Environment.GetEnvironmentVariable("PAYMENT_TEST_SQL_CONNECTION_STRING");
        var connection = new SqlConnectionStringBuilder(configuredConnection);
        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            // Una instancia propia evita depender del estado de MSSQLLocalDB.
            _localDbInstanceName = testDatabaseName;
            connection.DataSource = $@"(localdb)\{_localDbInstanceName}";
            connection.IntegratedSecurity = true;
            connection.TrustServerCertificate = true;
        }

        // Nunca migrar ni borrar una base existente indicada por la configuración.
        connection.InitialCatalog = testDatabaseName;
        ConnectionString = connection.ConnectionString;
    }

    public AppDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    public async Task InitializeAsync()
    {
        if (_localDbInstanceName is not null)
        {
            await RunLocalDbAsync("create", _localDbInstanceName);
            _localDbInstanceCreated = true;
            await RunLocalDbAsync("start", _localDbInstanceName);
        }

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_localDbInstanceName is not null && !_localDbInstanceCreated)
            return;

        try
        {
            await using var db = CreateDbContext();
            await db.Database.EnsureDeletedAsync();
        }
        finally
        {
            if (_localDbInstanceCreated)
            {
                await RunLocalDbAsync("stop", _localDbInstanceName!);
                await RunLocalDbAsync("delete", _localDbInstanceName!);
            }
        }
    }

    private static async Task RunLocalDbAsync(string operation, string instanceName)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("sqllocaldb")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList = { operation, instanceName }
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var messages = await Task.WhenAll(output, error);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"SqlLocalDB {operation} {instanceName} falló: {string.Join(Environment.NewLine, messages)}");
    }
}
