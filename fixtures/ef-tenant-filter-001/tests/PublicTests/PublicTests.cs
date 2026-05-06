using EfTenantFilter001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class PublicTests
{
    [Fact]
    public async Task ListForTenant_should_exclude_other_tenants()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BillingContext>().UseSqlite(connection).Options;
        await using var db = new BillingContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Invoices.AddRange(new Invoice { TenantId = "a", Amount = 10 }, new Invoice { TenantId = "b", Amount = 20 });
        await db.SaveChangesAsync();

        var invoices = await new InvoiceRepository(db).ListForTenantAsync("a");
        invoices.Should().ContainSingle().Which.TenantId.Should().Be("a");
    }
}
