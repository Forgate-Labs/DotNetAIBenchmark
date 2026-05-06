using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Create_should_trim_valid_name()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/products", new { name = "  Keyboard  ", price = 50m });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product!.Name.Should().Be("Keyboard");
    }

    [Fact]
    public async Task Create_should_reject_long_name()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/products", new { name = new string('x', 41), price = 10m });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
