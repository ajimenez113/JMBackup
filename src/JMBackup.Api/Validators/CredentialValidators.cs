using FluentValidation;
using JMBackup.Api.Contracts;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Validators;

public sealed class CredentialRequestValidator : AbstractValidator<CredentialRequest>
{
    public CredentialRequestValidator()
    {
        RuleFor(request => request.Alias).NotEmpty().MaximumLength(200);

        // Local incluida a propósito: una credencial de Local es para un recurso UNC
        // (ADR-008), no para el disco local en sí. SFTP se rechaza, igual que en
        // TaskPathRequestValidator: todavía no existe.
        RuleFor(request => request.BackendType)
            .Must(value => Enum.TryParse<BackendType>(value, out var type) && type != BackendType.Sftp)
            .WithMessage("SFTP todavía no está implementado.");

        RuleFor(request => request.Username).MaximumLength(200);
        RuleFor(request => request.Password).NotEmpty()
            .WithMessage("Hace falta una contraseña para guardar la credencial.");
    }
}

public sealed class CredentialUpdateRequestValidator : AbstractValidator<CredentialUpdateRequest>
{
    public CredentialUpdateRequestValidator()
    {
        RuleFor(request => request.Alias).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Username).MaximumLength(200);
    }
}
