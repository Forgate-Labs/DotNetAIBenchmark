using FluentAssertions;
using TestsMoneyRounding001.Core;

public sealed class HiddenTests
{
    [Fact]
    public void CalculateTotal_should_use_away_from_zero_rounding()
    {
        var total = new TaxCalculator().CalculateTotal([1.25m], 0.10m);
        total.Should().Be(1.38m);
    }
}
