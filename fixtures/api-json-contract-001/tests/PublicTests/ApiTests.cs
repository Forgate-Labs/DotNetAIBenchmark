using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Profile_should_use_public_contract_names()
    {
        var json = await factory.CreateClient().GetStringAsync("/profiles/1");
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("displayName", out var name).Should().BeTrue();
        name.GetString().Should().Be("Ada Lovelace");
    }
}
