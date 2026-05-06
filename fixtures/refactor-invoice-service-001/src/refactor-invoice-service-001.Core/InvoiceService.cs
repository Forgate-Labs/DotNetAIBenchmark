namespace RefactorInvoiceService001.Core;

public sealed record Invoice(int Id, string CustomerEmail, decimal Amount);

public interface IInvoiceRepository
{
    void Save(Invoice invoice);
}

public interface INotificationSender
{
    void Send(string to, string message);
}

public sealed class InvoiceService(IInvoiceRepository configuredRepository, INotificationSender configuredSender)
{
    public void Create(Invoice invoice)
    {
        var repository = new InMemoryInvoiceRepository();
        var sender = new SmtpEmailSender();
        repository.Save(invoice);
        sender.Send(invoice.CustomerEmail, $"Invoice {invoice.Id} created");
    }
}

public sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    public List<Invoice> Saved { get; } = [];
    public void Save(Invoice invoice) => Saved.Add(invoice);
}

public sealed class SmtpEmailSender : INotificationSender
{
    public void Send(string to, string message) => throw new InvalidOperationException("SMTP is not available in tests.");
}
