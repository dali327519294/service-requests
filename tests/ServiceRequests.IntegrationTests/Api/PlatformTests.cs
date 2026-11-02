using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class PlatformTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_returns_200_when_database_is_reachable()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_returns_503_when_database_is_unreachable()
    {
        // Port 1 refuses connections at once, so the check fails fast instead of waiting for a timeout
        using var unreachable = factory.WithWebHostBuilder(builder => builder.UseSetting(
            "ConnectionStrings:ServiceRequestsDb", "Server=127.0.0.1,1;Connect Timeout=2;TrustServerCertificate=True"));

        var response = await unreachable.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Unhandled_exception_returns_problem_details_without_internal_details()
    {
        var response = await factory.CreateClient().GetAsync("/test/throw");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("Secret internal detail", body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Current_user_comes_from_the_user_name_header()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana.levi");

        Assert.Equal("dana.levi", await client.GetStringAsync("/test/user"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("a-name-longer-than-one-hundred-characters-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    public async Task Missing_or_invalid_user_name_header_returns_400(string? userName)
    {
        var client = factory.CreateClient();
        if (userName is not null)
        {
            client.DefaultRequestHeaders.Add("X-User-Name", userName);
        }

        var response = await client.GetAsync("/test/user");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
