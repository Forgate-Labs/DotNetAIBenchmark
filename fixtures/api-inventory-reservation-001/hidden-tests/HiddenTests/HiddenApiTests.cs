using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Reservation_should_accept_exact_stock()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/inventory/book/reservations", new { quantity = 3 });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ReservationResponse>();
        body!.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task Reservation_should_reject_negative_quantity()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/inventory/pen/reservations", new { quantity = -1 });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
