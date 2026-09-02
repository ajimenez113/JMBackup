using System.Net;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using JMBackup.Api.Authentication;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace JMBackup.Api.IntegrationTests;

/// <summary>
/// Prueba <see cref="CredentialRequirementHandler"/> directamente (sin pasar por HTTP):
/// WebApplicationFactory/TestServer no simula una IP de cliente real
/// (<c>Connection.RemoteIpAddress</c> da null), así que el caso "header del escritorio
/// + conexión de loopback" —el único que la corrección de seguridad de la fase 9
/// sigue aceptando como escritorio— no se puede ejercitar por HTTP. El caso "header sin
/// loopback" sí se prueba por HTTP en <c>AuthEndpointsTests</c>.
/// </summary>
public class CredentialRequirementHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static CredentialRequirementHandler CreateHandler(AuthScope requireCredentialFor)
    {
        var settingsStore = Substitute.For<ISettingsStore>();
        var securityJson = JsonSerializer.Serialize(
            new SecuritySettings { Username = "admin", RequireCredentialFor = requireCredentialFor }, JsonOptions);
        settingsStore.GetAsync("Security", Arg.Any<CancellationToken>()).Returns(securityJson);

        return new CredentialRequirementHandler(new SettingsService(settingsStore));
    }

    private static AuthorizationHandlerContext CreateContext(bool sendDesktopHeader, IPAddress? remoteIpAddress)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteIpAddress;
        if (sendDesktopHeader)
        {
            httpContext.Request.Headers[CredentialRequirementHandler.DesktopClientHeaderName] = "Desktop";
        }

        return new AuthorizationHandlerContext([new CredentialRequirement()], new ClaimsPrincipal(new ClaimsIdentity()), httpContext);
    }

    [Fact]
    public async Task WebOnly_HeaderFromLoopback_IsTreatedAsDesktopAndNotRequired()
    {
        var handler = CreateHandler(AuthScope.WebOnly);
        var context = CreateContext(sendDesktopHeader: true, IPAddress.Loopback);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task WebOnly_HeaderFromNonLoopbackAddress_IsNotTreatedAsDesktopAndStillRequiresAuth()
    {
        var handler = CreateHandler(AuthScope.WebOnly);
        var context = CreateContext(sendDesktopHeader: true, IPAddress.Parse("192.168.1.50"));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AppOnly_HeaderFromLoopback_IsTreatedAsDesktopSoAuthIsRequired()
    {
        // "Solo aplicación" exige la credencial justo AL REVÉS que "solo web": para el
        // cliente de escritorio, no para el navegador. Con header+loopback (escritorio
        // real) y sin sesión autenticada, no debe tener éxito.
        var handler = CreateHandler(AuthScope.AppOnly);
        var context = CreateContext(sendDesktopHeader: true, IPAddress.Loopback);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AppOnly_NoHeaderEvenFromLoopback_IsNotTreatedAsDesktopSoAuthIsNotRequired()
    {
        var handler = CreateHandler(AuthScope.AppOnly);
        var context = CreateContext(sendDesktopHeader: false, IPAddress.Loopback);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
