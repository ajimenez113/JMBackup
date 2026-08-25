namespace JMBackup.Api.Contracts;

/// <summary>
/// La contraseña viaja en texto plano solo dentro de este pedido HTTPS, para cifrarla
/// con DPAPI antes de guardarla (CLAUDE.md §6): nunca se persiste ni se registra tal
/// cual.
/// </summary>
public sealed record CredentialRequest(string Alias, string? Username, string Password);

/// <summary>Nunca incluye el secreto cifrado ni la contraseña: alcanza con el alias para elegirla en la interfaz.</summary>
public sealed record CredentialResponse(int Id, string Alias, string? Username, DateTimeOffset CreatedAt);
