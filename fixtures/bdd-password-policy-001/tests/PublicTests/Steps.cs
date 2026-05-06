using BddPasswordPolicy001.Core;
using FluentAssertions;
using Reqnroll;

[Binding]
public sealed class Steps
{
    private string _password = string.Empty;
    private bool _valid;
    [Given("the password (.*)")]
    public void GivenPassword(string password) => _password = password;
    [When("the password is validated")]
    public void WhenValidated() => _valid = new PasswordPolicy().IsValid(_password);
    [Then("it should be invalid")]
    public void ThenInvalid() => _valid.Should().BeFalse();
}
