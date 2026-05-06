using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Quote_should_include_line_quantities()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/orders/quote", new
        {
            lines = new[]
            {
                new { sku = "A", quantity = 2, unitPrice = 10m },
                new { sku = "B", quantity = 1, unitPrice = 5m }
            }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OrderQuoteResponse>();
        body!.Total.Should().Be(25m);
    }
}
