namespace JMBackup.Api.Security;

/// <summary>Cabeceras de seguridad en toda respuesta (CLAUDE.md §6). HSTS lo agrega <c>UseHsts()</c> aparte.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "same-origin";
            headers["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' wss: ws:; frame-ancestors 'none'";

            return Task.CompletedTask;
        });

        return next(context);
    }
}
