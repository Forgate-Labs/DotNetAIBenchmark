using Microsoft.EntityFrameworkCore;

namespace EfTenantFilter001.Core;

public sealed class BillingContext(DbContextOptions<BillingContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
}

public sealed class Invoice
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class InvoiceRepository(BillingContext db)
{
    public Task<List<Invoice>> ListForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return db.Invoices.OrderBy(invoice => invoice.Id).ToListAsync(cancellationToken);
    }
}
