using FluentAssertions;
using TestsMoneyRounding001.Core;

public sealed class PublicTests
{
    [Fact]
    public void CalculateTotal_should_round_tax_once_for_subtotal()
    {
        var total = new TaxCalculator().CalculateTotal([0.05m, 0.05m], 0.10m);
        total.Should().Be(0.11m);
    }
}
