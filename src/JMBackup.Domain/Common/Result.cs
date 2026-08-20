namespace JMBackup.Domain.Common;

/// <summary>
/// Resultado de una operación que puede fallar de forma esperada. Se usa en vez de
/// lanzar excepciones para los fallos que forman parte del flujo normal del negocio
/// (ver <see cref="JMBackup.Domain.Exceptions.JMBackupException"/> para el contraste).
/// </summary>
/// <typeparam name="T">Tipo del valor que produce la operación cuando tiene éxito.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;

    internal Result(bool isSuccess, T? value, ResultError error)
    {
        if (isSuccess && error != ResultError.None)
        {
            throw new ArgumentException("Un resultado exitoso no puede llevar error.", nameof(error));
        }

        if (!isSuccess && error == ResultError.None)
        {
            throw new ArgumentException("Un resultado fallido debe llevar un error.", nameof(error));
        }

        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ResultError Error { get; }

    /// <summary>Valor del resultado. Lanza <see cref="InvalidOperationException"/> si el resultado falló.</summary>
    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("No se puede leer el valor de un resultado fallido.");

    public static implicit operator Result<T>(T value) => Result.Success(value);

    public static implicit operator Result<T>(ResultError error) => Result.Failure<T>(error);
}

/// <summary>
/// Fábricas de <see cref="Result{T}"/>. Viven en un tipo no genérico (en vez de métodos
/// estáticos dentro de <see cref="Result{T}"/>) porque la regla CA1000 lo exige: un
/// miembro estático en un tipo genérico obliga a los consumidores de otros lenguajes
/// .NET a repetir el argumento de tipo en cada llamada.
/// </summary>
public static class Result
{
    public static Result<T> Success<T>(T value) => new(true, value, ResultError.None);

    public static Result<T> Failure<T>(ResultError error) => new(false, default, error);
}
