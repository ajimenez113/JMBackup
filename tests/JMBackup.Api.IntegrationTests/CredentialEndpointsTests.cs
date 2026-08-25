using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JMBackup.Api.Contracts;
using static JMBackup.Api.IntegrationTests.AntiforgeryTestHelper;

namespace JMBackup.Api.IntegrationTests;

[Collection(ApiIntegrationTestGroup.Name)]
public sealed class CredentialEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public CredentialEndpointsTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateThenList_ReturnsTheCredentialWithoutExposingTheSecret()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/credentials", UriKind.Relative))
        {
            Content = JsonContent.Create(new CredentialRequest("NAS1", "NAS1\\usuario", "una-contraseña-cualquiera")),
        };
        createRequest.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);

        var createResponse = await client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await createResponse.Content.ReadAsStringAsync();
        body.Should().NotContain("una-contraseña-cualquiera");

        var created = await createResponse.Content.ReadFromJsonAsync<CredentialResponse>();
        created.Should().NotBeNull();
        created!.Alias.Should().Be("NAS1");
        created.Username.Should().Be("NAS1\\usuario");

        var listResponse = await client.GetAsync(new Uri("/api/credentials", UriKind.Relative));
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<List<CredentialResponse>>();
        list.Should().Contain(c => c.Id == created.Id && c.Alias == "NAS1");
    }

    [Fact]
    public async Task Create_WithoutPassword_IsRejected()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/credentials", UriKind.Relative))
        {
            Content = JsonContent.Create(new CredentialRequest("NAS2", "usuario", string.Empty)),
        };
        createRequest.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);

        var response = await client.SendAsync(createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
