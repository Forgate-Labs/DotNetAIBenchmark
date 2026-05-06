using FluentAssertions;
using RefactorInvoiceService001.Core;

public sealed class HiddenTests
{
    [Fact]
    public void Create_should_not_send_notification_when_repository_fails()
    {
        var sender = new FakeNotificationSender();
        var service = new InvoiceService(new FailingRepository(), sender);
        var act = () => service.Create(new Invoice(2, "grace@example.com", 20m));
        act.Should().Throw<InvalidOperationException>();
        sender.Messages.Should().BeEmpty();
    }

    private sealed class FailingRepository : IInvoiceRepository
    {
        public void Save(Invoice invoice) => throw new InvalidOperationException("write failed");
    }

    private sealed class FakeNotificationSender : INotificationSender
    {
        public List<(string To, string Message)> Messages { get; } = [];
        public void Send(string to, string message) => Messages.Add((to, message));
    }
}
