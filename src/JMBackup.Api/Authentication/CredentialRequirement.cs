using JMBackup.Application.Settings;
using Microsoft.AspNetCore.Authorization;

namespace JMBackup.Api.Authentication;

public sealed class CredentialRequirement : IAuthorizationRequirement;

/// <summary>
/// Exige sesión autenticada salvo que RF-101 esté en "ninguna" o no haya credencial
/// configurada todavía (primer arranque). No distingue "solo web" de "solo
/// aplicación": en el hito 1 el escritorio habla con la API exactamente igual que un
/// navegador (ADR-003), así que esa distinción no tiene con qué implementarse hasta
/// que exista el puente nativo de la fase 4.
/// </summary>
public sealed class CredentialRequirementHandler(SettingsService settingsService) : AuthorizationHandler<CredentialRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CredentialRequirement requirement)
    {
        var security = await settingsService.GetSecurityAsync(CancellationToken.None).ConfigureAwait(false);

        if (security.RequireCredentialFor == AuthScope.None || string.IsNullOrEmpty(security.Username))
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
