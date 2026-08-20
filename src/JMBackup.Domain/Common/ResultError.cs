namespace JMBackup.Domain.Common;

/// <summary>
/// Código y mensaje de un fallo esperado (no una excepción): credencial inválida,
/// archivo bloqueado, host inalcanzable, etc. Lo produce y consume <see cref="Result{T}"/>.
/// Se llama <c>ResultError</c> y no <c>Error</c> porque ese nombre choca con la palabra
/// reservada de otros lenguajes .NET (regla CA1716).
/// </summary>
public sealed record ResultError(string Code, string Message)
{
    /// <summary>Ausencia de error, usado internamente por un <see cref="Result{T}"/> exitoso.</summary>
    public static readonly ResultError None = new(string.Empty, string.Empty);
}
