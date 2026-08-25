using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Contracts;

public static class CredentialMappings
{
    public static Credential ToEntity(this CredentialRequest request, INetworkCredentialProtector protector) => new()
    {
        Alias = request.Alias,
        BackendType = BackendType.Local,
        Username = request.Username,
        EncryptedSecret = protector.Protect(request.Password),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public static CredentialResponse ToResponse(this Credential credential) =>
        new(credential.Id, credential.Alias, credential.Username, credential.CreatedAt);
}
