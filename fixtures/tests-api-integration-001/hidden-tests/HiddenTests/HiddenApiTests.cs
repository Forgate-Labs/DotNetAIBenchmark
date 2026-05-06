using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_should_return_json_content_type()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.EnumerateObject().Should().ContainSingle();
    }
}
