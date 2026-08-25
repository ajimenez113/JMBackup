using FluentValidation;
using JMBackup.Api.Contracts;
using JMBackup.Application.Auth;
using JMBackup.Application.Settings;

namespace JMBackup.Api.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Username).NotEmpty();
        RuleFor(request => request.Password).NotEmpty();
    }
}

/// <summary>RF-102/RF-105: exponer sin credencial exige escribir la frase de confirmación exacta.</summary>
public sealed class SecuritySettingsRequestValidator : AbstractValidator<SecuritySettingsRequest>
{
    private const string RiskConfirmationPhrase = "ENTIENDO EL RIESGO";

    public SecuritySettingsRequestValidator()
    {
        RuleFor(request => request.NewPassword)
            .Must(password => password is null || PasswordPolicy.IsValid(password))
            .WithMessage("La contraseña necesita mínimo 8 caracteres, una mayúscula, una minúscula y un número.");

        RuleFor(request => request.RequireCredentialFor).Must(value => Enum.TryParse<AuthScope>(value, out _));

        RuleFor(request => request.RiskConfirmationPhrase)
            .Equal(RiskConfirmationPhrase)
            .When(request => request.RequireCredentialFor == nameof(AuthScope.None) && request.AllowUnauthenticatedLan)
            .WithMessage($"Para exponer sin credencial hay que escribir \"{RiskConfirmationPhrase}\".");
    }
}

public sealed class WebSettingsRequestValidator : AbstractValidator<WebSettingsRequest>
{
    public WebSettingsRequestValidator()
    {
        RuleFor(request => request.Port).InclusiveBetween(1, 65535);
        RuleFor(request => request.ListenAddress).NotEmpty();
    }
}

public sealed class GeneralSettingsRequestValidator : AbstractValidator<GeneralSettingsRequest>
{
    public GeneralSettingsRequestValidator()
    {
        RuleFor(request => request.HistoryRetentionDays).GreaterThanOrEqualTo(1);
    }
}

public sealed class TransferSettingsRequestValidator : AbstractValidator<TransferSettingsRequest>
{
    public TransferSettingsRequestValidator()
    {
        RuleFor(request => request.MaxParallelTransfers).InclusiveBetween(1, 16);
        RuleFor(request => request.BlockSizeBytes).GreaterThan(0);
        RuleFor(request => request.GlobalBandwidthLimitBytesPerSecond).GreaterThan(0)
            .When(request => request.GlobalBandwidthLimitBytesPerSecond is not null);
    }
}
