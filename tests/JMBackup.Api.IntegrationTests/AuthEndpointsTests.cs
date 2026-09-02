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
    [InlineData("AppOnly", false, HttpStatusCode.OK)] // pedido sin header, "solo aplicación" no lo exige
    [InlineData("AppOnly", true, HttpStatusCode.OK)] // header solo, sin loopback: NO cuenta como escritorio (fix de seguridad) — se sigue sin exigir
    [InlineData("WebOnly", false, HttpStatusCode.Unauthorized)] // pedido sin header, "solo web" sí lo exige
    [InlineData("WebOnly", true, HttpStatusCode.Unauthorized)] // header solo, sin loopback: NO cuenta como escritorio, "solo web" lo sigue exigiendo
    public async Task RequireCredentialFor_HeaderAloneWithoutLoopback_IsNeverTreatedAsDesktop(
        string requireCredentialFor, bool sendDesktopHeader, HttpStatusCode expectedStatus)
    {
        // Fase 4: RF-101 permite exigir la credencial "solo web" o "solo aplicación" por
        // separado. El shell WPF agrega el header
        // CredentialRequirementHandler.DesktopClientHeaderName a cada pedido — pero,
        // desde la corrección de seguridad de la fase 9 (el header por sí solo era
        // falsificable por cualquiera en la LAN), TAMBIÉN hace falta que la conexión sea
        // de loopback para contar como "escritorio". WebApplicationFactory/TestServer no
        // simula una IP de cliente real (Connection.RemoteIpAddress da null), así que
        // esta prueba —sin querer— es exactamente el caso "header sin loopback": prueba
        // que ya NO alcanza con el header solo. El caso "header + loopback sí cuenta"
        // se prueba aparte, sin pasar por HTTP, en CredentialRequirementHandlerTests.
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

    [Fact]
    public async Task ProgressHub_Negotiate_WithoutSession_RequiresAuthentication()
    {
        // El hub de progreso difunde rutas de archivo en vivo (RF-03) — antes de esta
        // prueba, no exigía sesión, a diferencia de todos los demás endpoints.
        using var isolatedFactory = new ApiWebApplicationFactory();
        using var setupClient = isolatedFactory.CreateSecureClient();
        var setupToken = await GetAntiforgeryTokenAsync(setupClient);

        using var putRequest = new HttpRequestMessage(HttpMethod.Put, new Uri("/api/settings/security", UriKind.Relative))
        {
            Content = JsonContent.Create(new SecuritySettingsRequest(
                "admin", "Abcd1234!", "Both", SessionInactivityMinutes: 30, AllowUnauthenticatedLan: false, RiskConfirmationPhrase: null)),
        };
        putRequest.Headers.Add("X-XSRF-TOKEN", setupToken);
        (await setupClient.SendAsync(putRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var anonymousClient = isolatedFactory.CreateClient();
        var negotiateResponse = await anonymousClient.PostAsync(new Uri("/hubs/progress/negotiate?negotiateVersion=1", UriKind.Relative), content: null);

        negotiateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
