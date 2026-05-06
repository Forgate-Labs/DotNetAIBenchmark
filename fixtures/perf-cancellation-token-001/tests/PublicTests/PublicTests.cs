using FluentAssertions;
using PerfCancellationToken001.Core;

public sealed class PublicTests
{
    [Fact]
    public async Task Process_should_observe_cancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = () => new BatchProcessor().ProcessAsync(Enumerable.Range(1, 10), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
