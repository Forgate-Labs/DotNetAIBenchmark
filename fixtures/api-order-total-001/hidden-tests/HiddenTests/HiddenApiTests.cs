using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Quote_should_reject_empty_orders()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/orders/quote", new { lines = Array.Empty<object>() });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Quote_should_reject_zero_quantity()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/orders/quote", new { lines = new[] { new { sku = "A", quantity = 0, unitPrice = 10m } } });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
