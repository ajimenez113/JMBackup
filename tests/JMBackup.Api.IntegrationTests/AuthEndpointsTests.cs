using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JMBackup.Api.Contracts;

namespace JMBackup.Api.IntegrationTests;

[Collection(ApiIntegrationTestGroup.Name)]
public sealed class AuthEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthEndpointsTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Session_NoCredentialConfigured_TaskEndpointsAreReachableWithoutLoggingIn()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/tasks", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongCredentials_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(new Uri("/api/auth/login", UriKind.Relative), new LoginRequest("nadie", "loquesea"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_WhenNotLoggedIn_ReportsNotAuthenticated()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/auth/session", UriKind.Relative));
        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();

        session!.IsAuthenticated.Should().BeFalse();
    }
}
