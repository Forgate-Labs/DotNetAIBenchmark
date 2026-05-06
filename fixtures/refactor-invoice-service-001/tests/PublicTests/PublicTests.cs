using FluentAssertions;
using RefactorInvoiceService001.Core;

public sealed class PublicTests
{
    [Fact]
    public void Create_should_be_testable_without_smtp()
    {
        var repository = new FakeInvoiceRepository();
        var sender = new FakeNotificationSender();
        var service = new InvoiceService(repository, sender);

        service.Create(new Invoice(1, "ada@example.com", 10m));

        repository.Saved.Should().ContainSingle();
        sender.Messages.Should().ContainSingle(message => message.To == "ada@example.com");
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public List<Invoice> Saved { get; } = [];
        public void Save(Invoice invoice) => Saved.Add(invoice);
    }

    private sealed class FakeNotificationSender : INotificationSender
    {
        public List<(string To, string Message)> Messages { get; } = [];
        public void Send(string to, string message) => Messages.Add((to, message));
    }
}
