using FluentAssertions;
using PerfStringNormalizer001.Core;

public sealed class PublicTests
{
    [Fact]
    public void Generate_should_create_basic_slug()
    {
        new SlugGenerator().Generate("Hello, World!").Should().Be("hello-world");
    }
}
