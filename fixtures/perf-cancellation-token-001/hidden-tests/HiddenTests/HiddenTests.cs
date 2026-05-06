using FluentAssertions;
using PerfCancellationToken001.Core;

public sealed class HiddenTests
{
    [Fact]
    public async Task Process_should_cancel_during_work()
    {
        using var cts = new CancellationTokenSource(25);
        var act = () => new BatchProcessor().ProcessAsync(Enumerable.Range(1, 100), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
