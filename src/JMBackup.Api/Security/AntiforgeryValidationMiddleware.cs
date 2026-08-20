using Microsoft.AspNetCore.Antiforgery;

namespace JMBackup.Api.Security;

/// <summary>
/// Antiforgery en operaciones de escritura (CLAUDE.md §6). El login queda exento
/// porque todavía no hay sesión de la que sacar el token; el resto de los métodos que
/// mutan estado sí lo exige, vía la cabecera <c>X-XSRF-TOKEN</c> que entrega
/// <c>GET /api/antiforgery/token</c>.
/// </summary>
public sealed class AntiforgeryValidationMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var isExempt = context.Request.Path.StartsWithSegments("/api/auth/login")
            || context.Request.Path.StartsWithSegments("/hubs");

        if (!isExempt && MutatingMethods.Contains(context.Request.Method))
        {
            await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        }

        await next(context).ConfigureAwait(false);
    }
}
