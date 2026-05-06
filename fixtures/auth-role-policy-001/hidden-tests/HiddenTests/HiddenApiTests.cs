using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task User_role_should_be_forbidden()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/reports");
        request.Headers.Add("X-Role", "User");
        var response = await factory.CreateClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
