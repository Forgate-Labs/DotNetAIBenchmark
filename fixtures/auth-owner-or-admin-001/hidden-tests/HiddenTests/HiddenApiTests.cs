using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Other_user_should_be_forbidden()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/notes/2");
        request.Headers.Add("X-User", "ada");
        request.Headers.Add("X-Role", "User");
        var response = await factory.CreateClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_should_delete_any_note()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/notes/2");
        request.Headers.Add("X-User", "root");
        request.Headers.Add("X-Role", "Admin");
        var response = await factory.CreateClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
