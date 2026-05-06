namespace PerfCancellationToken001.Core;

public sealed class BatchProcessor
{
    public async Task<int> ProcessAsync(IEnumerable<int> items, CancellationToken cancellationToken = default)
    {
        var processed = 0;
        foreach (var item in items)
        {
            await Task.Delay(10);
            processed += item;
        }
        return processed;
    }
}
