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
    public string ConnectionString { get; } = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("PAYMENT_TEST_SQL_CONNECTION_STRING")
        ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True")
    {
        // Nunca migrar ni borrar una base existente indicada por la configuración.
        InitialCatalog = $"PaymentTests_{Guid.NewGuid():N}"
    }.ConnectionString;

    public AppDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
    }
}
