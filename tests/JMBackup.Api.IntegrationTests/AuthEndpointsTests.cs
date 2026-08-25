using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JMBackup.Api.Authentication;
using JMBackup.Api.Contracts;
using static JMBackup.Api.IntegrationTests.AntiforgeryTestHelper;

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

    [Theory]
    [InlineData("AppOnly", false, HttpStatusCode.OK)] // pedido de navegador (sin header), "solo aplicación" no lo exige
    [InlineData("AppOnly", true, HttpStatusCode.Unauthorized)] // pedido del shell, "solo aplicación" sí lo exige
    [InlineData("WebOnly", false, HttpStatusCode.Unauthorized)] // pedido de navegador, "solo web" sí lo exige
    [InlineData("WebOnly", true, HttpStatusCode.OK)] // pedido del shell, "solo web" no lo exige
    public async Task RequireCredentialFor_DistinguishesDesktopFromBrowser_ByHeader(
        string requireCredentialFor, bool sendDesktopHeader, HttpStatusCode expectedStatus)
    {
        // Fase 4: RF-101 permite exigir la credencial "solo web" o "solo aplicación" por
        // separado — la API lo resuelve por el header que agrega el shell WPF
        // (CredentialRequirementHandler.DesktopClientHeaderName), nunca presente en un
        // navegador real.
        //
        // Esta prueba configura una credencial real, algo que ninguna otra prueba de
        // esta colección hace ni puede deshacer después (PutSecurityAsync no permite
        // "desconfigurar" el usuario, solo reemplazarlo). Por eso arma su propia
        // ApiWebApplicationFactory en vez de usar la que comparte toda la colección —
        // usa su propio directorio temporal y no ensucia el estado de las demás
        // pruebas. Sigue en la misma colección para no correr en paralelo con ellas
        // (la variable de entorno que usa ApiWebApplicationFactory es del proceso).
        using var isolatedFactory = new ApiWebApplicationFactory();
        using var setupClient = isolatedFactory.CreateSecureClient();
        var setupToken = await GetAntiforgeryTokenAsync(setupClient);

        using var putRequest = new HttpRequestMessage(HttpMethod.Put, new Uri("/api/settings/security", UriKind.Relative))
        {
            Content = JsonContent.Create(new SecuritySettingsRequest(
                "admin", "Abcd1234!", requireCredentialFor, SessionInactivityMinutes: 30, AllowUnauthenticatedLan: false, RiskConfirmationPhrase: null)),
        };
        putRequest.Headers.Add("X-XSRF-TOKEN", setupToken);
        (await setupClient.SendAsync(putRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var probeClient = isolatedFactory.CreateClient();
        using var probeRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/tasks", UriKind.Relative));
        if (sendDesktopHeader)
        {
            probeRequest.Headers.Add(CredentialRequirementHandler.DesktopClientHeaderName, "Desktop");
        }

        var probeResponse = await probeClient.SendAsync(probeRequest);

        probeResponse.StatusCode.Should().Be(expectedStatus);
    }
}
