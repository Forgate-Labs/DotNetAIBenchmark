using EfTenantFilter001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class HiddenTests
{
    [Fact]
    public async Task ListForTenant_should_return_empty_for_unknown_tenant()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BillingContext>().UseSqlite(connection).Options;
        await using var db = new BillingContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Invoices.Add(new Invoice { TenantId = "a", Amount = 10 });
        await db.SaveChangesAsync();

        var invoices = await new InvoiceRepository(db).ListForTenantAsync("missing");
        invoices.Should().BeEmpty();
    }
}
