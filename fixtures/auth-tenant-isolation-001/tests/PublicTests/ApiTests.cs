using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Document_should_not_cross_tenant_boundary()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/documents/1");
        request.Headers.Add("X-Tenant", "tenant-b");
        var response = await factory.CreateClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
