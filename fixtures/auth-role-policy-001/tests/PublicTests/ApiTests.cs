using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Admin_role_should_access_report()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/reports");
        request.Headers.Add("X-Role", "Admin");
        var response = await factory.CreateClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
