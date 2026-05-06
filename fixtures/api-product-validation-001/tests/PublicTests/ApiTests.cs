using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Create_should_reject_blank_name()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/products", new { name = "   ", price = 12m });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
