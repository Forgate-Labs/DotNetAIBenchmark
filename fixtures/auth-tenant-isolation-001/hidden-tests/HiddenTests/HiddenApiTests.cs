using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class HiddenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Document_should_be_visible_to_own_tenant()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/documents/1");
        request.Headers.Add("X-Tenant", "tenant-a");
        var response = await factory.CreateClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<Document>();
        document!.TenantId.Should().Be("tenant-a");
    }

    [Fact]
    public async Task Missing_tenant_should_be_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/documents/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
