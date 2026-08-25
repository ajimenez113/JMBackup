using FluentValidation;
using JMBackup.Api.Contracts;

namespace JMBackup.Api.Validators;

public sealed class CredentialRequestValidator : AbstractValidator<CredentialRequest>
{
    public CredentialRequestValidator()
    {
        RuleFor(request => request.Alias).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Username).MaximumLength(200);
        RuleFor(request => request.Password).NotEmpty()
            .WithMessage("Hace falta una contraseña para guardar la credencial.");
    }
}
