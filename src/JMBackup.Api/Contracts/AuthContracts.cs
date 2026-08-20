namespace JMBackup.Api.Contracts;

public sealed record LoginRequest(string Username, string Password);

public sealed record SessionResponse(bool IsAuthenticated, string? Username);
