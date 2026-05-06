using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Reservation_should_conflict_when_stock_is_insufficient()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/inventory/book/reservations", new { quantity = 4 });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
