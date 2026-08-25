using JMBackup.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace JMBackup.Api.Authentication;

public sealed class CredentialRequirement : IAuthorizationRequirement;

/// <summary>
/// Exige sesión autenticada salvo que RF-101 esté en "ninguna" o no haya credencial
/// configurada todavía (primer arranque). RF-101 también permite exigirla "solo web"
/// o "solo aplicación": la distingue el header <see cref="DesktopClientHeaderName"/>,
/// que el shell WPF agrega a cada pedido hecho desde el WebView2 (fase 4) — un
/// navegador normal nunca lo manda.
/// </summary>
public sealed class CredentialRequirementHandler(SettingsService settingsService) : AuthorizationHandler<CredentialRequirement>
{
    /// <summary>
    /// Coincide con el header que agrega <c>NativeBridgeHandler</c> en
    /// JMBackup.Desktop — no hay un proyecto compartido entre la API y el shell WPF
    /// para poner esta constante en un solo lugar sin agregar una referencia nueva
    /// solo para esto.
    /// </summary>
    public const string DesktopClientHeaderName = "X-JMBackup-Client";

    private const string DesktopClientHeaderValue = "Desktop";

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CredentialRequirement requirement)
    {
        var security = await settingsService.GetSecurityAsync(CancellationToken.None).ConfigureAwait(false);

        var isDesktopClient = context.Resource is HttpContext httpContext
            && httpContext.Request.Headers[DesktopClientHeaderName] == DesktopClientHeaderValue;

        var requiresAuth = security.RequireCredentialFor switch
        {
            AuthScope.None => false,
            AuthScope.WebOnly => !isDesktopClient,
            AuthScope.AppOnly => isDesktopClient,
            _ => true, // AuthScope.Both
        };

        if (!requiresAuth || string.IsNullOrEmpty(security.Username))
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }
    }
}
