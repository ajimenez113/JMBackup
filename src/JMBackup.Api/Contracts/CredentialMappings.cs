using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Contracts;

public static class CredentialMappings
{
    public static Credential ToEntity(this CredentialRequest request, INetworkCredentialProtector protector) => new()
    {
        Alias = request.Alias,
        BackendType = Enum.Parse<BackendType>(request.BackendType),
        Username = request.Username,
        EncryptedSecret = protector.Protect(request.Password),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public static void ApplyTo(this CredentialUpdateRequest request, Credential credential, INetworkCredentialProtector protector)
    {
        credential.Alias = request.Alias;
        credential.Username = request.Username;
        if (!string.IsNullOrEmpty(request.Password))
        {
            credential.EncryptedSecret = protector.Protect(request.Password);
        }
    }

    public static CredentialResponse ToResponse(this Credential credential) =>
        new(credential.Id, credential.Alias, credential.BackendType.ToString(), credential.Username, credential.CreatedAt);
}
