using Microsoft.EntityFrameworkCore;

namespace EfNPlusOne001.Core;

public sealed class SalesContext(DbContextOptions<SalesContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
}

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
}

public sealed record CustomerSummary(string Name, int OrderCount);

public sealed class CustomerSummaryRepository(SalesContext db)
{
    public async Task<List<CustomerSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var customers = await db.Customers.OrderBy(customer => customer.Id).ToListAsync(cancellationToken);
        var summaries = new List<CustomerSummary>();
        foreach (var customer in customers)
        {
            var count = await db.Orders.CountAsync(order => order.CustomerId == customer.Id, cancellationToken);
            summaries.Add(new CustomerSummary(customer.Name, count));
        }
        return summaries;
    }
}
