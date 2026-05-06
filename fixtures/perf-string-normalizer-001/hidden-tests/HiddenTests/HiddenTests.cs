using FluentAssertions;
using PerfStringNormalizer001.Core;

public sealed class HiddenTests
{
    [Fact]
    public void Generate_should_keep_allocations_reasonable_for_large_input()
    {
        var input = string.Join(' ', Enumerable.Repeat("Hello", 5000));
        var generator = new SlugGenerator();
        _ = generator.Generate("warm up");
        var before = GC.GetAllocatedBytesForCurrentThread();
        var slug = generator.Generate(input);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        slug.Should().StartWith("hello-hello");
        allocated.Should().BeLessThan(1_000_000);
    }
}
