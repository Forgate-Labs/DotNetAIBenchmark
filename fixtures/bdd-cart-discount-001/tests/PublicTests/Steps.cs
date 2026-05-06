using BddCartDiscount001.Core;
using FluentAssertions;
using Reqnroll;

[Binding]
public sealed class Steps
{
    private decimal _subtotal;
    private decimal _total;

    [Given("a cart subtotal of (.*)")]
    public void GivenSubtotal(decimal subtotal) => _subtotal = subtotal;

    [When("the total is calculated")]
    public void WhenCalculated() => _total = new CartCalculator().CalculateTotal(_subtotal);

    [Then("the total should be (.*)")]
    public void ThenTotal(decimal total) => _total.Should().Be(total);
}
