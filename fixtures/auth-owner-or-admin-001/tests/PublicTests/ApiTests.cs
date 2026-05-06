using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Owner_should_delete_own_note()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/notes/1");
        request.Headers.Add("X-User", "ada");
        request.Headers.Add("X-Role", "User");
        var response = await factory.CreateClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
