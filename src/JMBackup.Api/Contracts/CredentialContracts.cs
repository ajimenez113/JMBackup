namespace JMBackup.Api.Contracts;

/// <summary>
/// La contraseña viaja en texto plano solo dentro de este pedido HTTPS, para cifrarla
/// con DPAPI antes de guardarla (CLAUDE.md §6): nunca se persiste ni se registra tal
/// cual.
/// </summary>
public sealed record CredentialRequest(string Alias, string BackendType, string? Username, string Password);

/// <summary>
/// Edición: a diferencia de <see cref="CredentialRequest"/>, la contraseña es opcional
/// — en blanco significa "no cambiarla", así que nunca hace falta conocer el secreto
/// actual para renombrar una credencial o corregir el usuario. El tipo de backend no
/// se puede cambiar: una credencial se crea para un tipo y queda así.
/// </summary>
public sealed record CredentialUpdateRequest(string Alias, string? Username, string? Password);

/// <summary>Nunca incluye el secreto cifrado ni la contraseña: alcanza con el alias para elegirla en la interfaz.</summary>
public sealed record CredentialResponse(int Id, string Alias, string BackendType, string? Username, DateTimeOffset CreatedAt);
