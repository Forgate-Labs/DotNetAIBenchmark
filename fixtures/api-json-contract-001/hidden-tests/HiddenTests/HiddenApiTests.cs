using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Profile_should_not_expose_internal_names()
    {
        var json = await factory.CreateClient().GetStringAsync("/profiles/1");
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("name", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("active", out _).Should().BeFalse();
        doc.RootElement.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Missing_profile_should_return_not_found()
    {
        var response = await factory.CreateClient().GetAsync("/profiles/999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
