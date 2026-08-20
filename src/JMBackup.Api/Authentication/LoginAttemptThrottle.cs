using System.Collections.Concurrent;

namespace JMBackup.Api.Authentication;

/// <summary>
/// Espera creciente en el login (RF-105/106 y el criterio de aceptación de seis
/// intentos fallidos). Cada fallo aumenta la duración del bloqueo siguiente; un login
/// correcto lo reinicia. Se guarda por IP de origen, en memoria — no hace falta
/// persistirlo, un reinicio del servicio ya corta cualquier ataque en curso.
/// </summary>
public sealed class LoginAttemptThrottle(TimeProvider timeProvider)
{
    private static readonly TimeSpan[] LockoutDurations =
    [
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), TimeSpan.FromMinutes(5),
    ];

    private readonly ConcurrentDictionary<string, AttemptState> _attempts = new();

    public TimeSpan? GetRemainingLockout(string key)
    {
        if (!_attempts.TryGetValue(key, out var state))
        {
            return null;
        }

        var remaining = state.LockedUntil - timeProvider.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public void RecordFailure(string key)
    {
        _attempts.AddOrUpdate(
            key,
            _ => new AttemptState(1, timeProvider.GetUtcNow() + LockoutDurations[0]),
            (_, existing) =>
            {
                var count = existing.FailureCount + 1;
                var durationIndex = Math.Min(count - 1, LockoutDurations.Length - 1);
                return new AttemptState(count, timeProvider.GetUtcNow() + LockoutDurations[durationIndex]);
            });
    }

    public void RecordSuccess(string key) => _attempts.TryRemove(key, out _);

    private sealed record AttemptState(int FailureCount, DateTimeOffset LockedUntil);
}
