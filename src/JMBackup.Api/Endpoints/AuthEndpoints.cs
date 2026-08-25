using System.Security.Claims;
using JMBackup.Api.Authentication;
using JMBackup.Api.Contracts;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using JMBackup.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace JMBackup.Api.Endpoints;

/// <summary>RF-100 a RF-107: credencial, sesión, bitácora de accesos.</summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting("login")
            .Produces<SessionResponse>().Produces(StatusCodes.Status401Unauthorized);
        Func<HttpContext, Task<IResult>> logoutHandler = LogoutAsync;
        group.MapPost("/logout", logoutHandler);
        group.MapGet("/session", GetSessionAsync).AllowAnonymous().Produces<SessionResponse>();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        SettingsService settingsService,
        IPasswordHasher passwordHasher,
        LoginAttemptThrottle throttle,
        IAuditLogRepository auditLog,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        if (throttle.GetRemainingLockout(clientIp) is { } lockout)
        {
            return Results.Json(
                new ErrorResponse($"Demasiados intentos. Esperá {lockout.TotalSeconds:F0} segundos."),
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var security = await settingsService.GetSecurityAsync(cancellationToken).ConfigureAwait(false);

        var isValid = security.Username is not null
            && security.PasswordHash is not null
            && string.Equals(security.Username, request.Username, StringComparison.Ordinal)
            && passwordHasher.Verify(request.Password, security.PasswordHash);

        await auditLog.AddAsync(new AuditLogEntry
        {
            Timestamp = timeProvider.GetUtcNow(),
            Actor = request.Username,
            SourceIp = clientIp,
            Action = isValid ? "login.success" : "login.failure",
        }, cancellationToken).ConfigureAwait(false);

        if (!isValid)
        {
            throttle.RecordFailure(clientIp);
            return Results.Unauthorized();
        }

        throttle.RecordSuccess(clientIp);

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, request.Username)], CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity)).ConfigureAwait(false);

        return Results.Ok(new SessionResponse(true, request.Username));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return Results.Ok();
    }

    private static IResult GetSessionAsync(HttpContext httpContext)
    {
        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
        return Results.Ok(new SessionResponse(isAuthenticated, isAuthenticated ? httpContext.User.Identity?.Name : null));
    }
}
