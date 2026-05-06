using EfNPlusOne001.Core;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class PublicTests
{
    [Fact]
    public async Task List_should_return_order_counts()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SalesContext>().UseSqlite(connection).Options;
        await using var db = new SalesContext(options);
        await db.Database.EnsureCreatedAsync();
        db.Customers.AddRange(new Customer { Name = "Ada" }, new Customer { Name = "Grace" });
        db.Orders.AddRange(new Order { CustomerId = 1 }, new Order { CustomerId = 1 });
        await db.SaveChangesAsync();

        var summaries = await new CustomerSummaryRepository(db).ListAsync();
        summaries.Should().ContainEquivalentOf(new CustomerSummary("Ada", 2));
        summaries.Should().ContainEquivalentOf(new CustomerSummary("Grace", 0));
    }
}
