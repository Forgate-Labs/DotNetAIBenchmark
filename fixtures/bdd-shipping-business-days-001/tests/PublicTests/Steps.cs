using BddShippingBusinessDays001.Core;
using FluentAssertions;
using Reqnroll;

[Binding]
public sealed class Steps
{
    private DateOnly _start;
    private DateOnly _result;
    [Given("the start date (.*)")]
    public void GivenStart(string date) => _start = DateOnly.Parse(date);
    [When("(.*) business days are added")]
    public void WhenAdded(int days) => _result = new BusinessDayCalculator().AddBusinessDays(_start, days);
    [Then("the result should be (.*)")]
    public void ThenResult(string date) => _result.Should().Be(DateOnly.Parse(date));
}
