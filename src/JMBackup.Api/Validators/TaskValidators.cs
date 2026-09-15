using FluentValidation;
using JMBackup.Api.Contracts;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;

namespace JMBackup.Api.Validators;

/// <summary>Valida el formato del contrato. La unicidad del nombre (RF-10) es una regla de negocio: la resuelve <c>TaskService</c>.</summary>
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Mode).Must(BeAnAllowedMode)
            .WithMessage("Los modos \"solo enviar\" y \"solo recibir\" son del hito 2.");
        RuleFor(request => request.OrderStrategy).Must(value => Enum.TryParse<OrderStrategy>(value, out _));
        RuleFor(request => request.VerifyLevel).Must(value => Enum.TryParse<VerifyLevel>(value, out _));
    }

    internal static bool BeAnAllowedMode(string value) =>
        Enum.TryParse<BackupMode>(value, out var mode) && mode is BackupMode.Incremental or BackupMode.Mirror;
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Mode).Must(CreateTaskRequestValidator.BeAnAllowedMode)
            .WithMessage("Los modos \"solo enviar\" y \"solo recibir\" son del hito 2.");
        RuleFor(request => request.OrderStrategy).Must(value => Enum.TryParse<OrderStrategy>(value, out _));
        RuleFor(request => request.VerifyLevel).Must(value => Enum.TryParse<VerifyLevel>(value, out _));
    }
}

/// <summary>
/// El formato de <see cref="TaskPathRequest.Path"/> depende del backend (ADR-006):
/// ruta absoluta local/UNC para <see cref="BackendType.Local"/>,
/// "servidor[:puerto]/ruta" para <see cref="BackendType.Ftp"/>,
/// "bucket/prefijo" para <see cref="BackendType.S3"/> (ver StorageBackendFactory, que
/// interpreta el mismo formato). SFTP se rechaza explícitamente: todavía no existe.
/// </summary>
public sealed class TaskPathRequestValidator : AbstractValidator<TaskPathRequest>
{
    public TaskPathRequestValidator(Security.PathValidationService pathValidation, ICredentialRepository credentialRepository)
    {
        RuleFor(request => request.Role).Must(value => Enum.TryParse<TaskPathRole>(value, out _));

        RuleFor(request => request.BackendType)
            .Must(value => Enum.TryParse<BackendType>(value, out var type) && type != BackendType.Sftp)
            .WithMessage("SFTP todavía no está implementado.");

        RuleFor(request => request.Path).Must(path => pathValidation.Validate(path).IsSuccess)
            .When(request => request.BackendType == nameof(BackendType.Local))
            .WithMessage("La ruta no es válida o contiene \"..\".");

        RuleFor(request => request.Path).Must(BeAValidFtpPath)
            .When(request => request.BackendType == nameof(BackendType.Ftp))
            .WithMessage("La ruta FTP debe tener el formato \"servidor[:puerto]/ruta\".");

        RuleFor(request => request.Path).Must(BeAValidS3Path)
            .When(request => request.BackendType == nameof(BackendType.S3))
            .WithMessage("La ruta S3 debe tener el formato \"bucket/prefijo\" (el prefijo es opcional).");

        RuleFor(request => request.Region).NotEmpty()
            .When(request => request.BackendType == nameof(BackendType.S3))
            .WithMessage("Una ruta S3 necesita una región.");

        RuleFor(request => request.CredentialId).NotNull()
            .When(request => request.BackendType is nameof(BackendType.Ftp) or nameof(BackendType.S3))
            .WithMessage("Una ruta remota necesita una credencial configurada.");

        RuleFor(request => request.CredentialId)
            .MustAsync(async (credentialId, cancellationToken) => credentialId is null
                || await credentialRepository.FindAsync(credentialId.Value, cancellationToken).ConfigureAwait(false) is not null)
            .WithMessage("La credencial seleccionada no existe.");
    }

    private static bool BeAValidFtpPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
        {
            return false;
        }

        var trimmed = path.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        var hostAndPort = firstSlash < 0 ? trimmed : trimmed[..firstSlash];

        var colonIndex = hostAndPort.IndexOf(':');
        if (colonIndex < 0)
        {
            return hostAndPort.Length > 0;
        }

        var host = hostAndPort[..colonIndex];
        var portText = hostAndPort[(colonIndex + 1)..];
        return host.Length > 0 && int.TryParse(portText, out var port) && port is > 0 and <= 65535;
    }

    private static bool BeAValidS3Path(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains(".."))
        {
            return false;
        }

        var trimmed = path.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        var bucket = firstSlash < 0 ? trimmed : trimmed[..firstSlash];
        return bucket.Length is >= 3 and <= 63;
    }
}

public sealed class ExclusionRequestValidator : AbstractValidator<ExclusionRequest>
{
    public ExclusionRequestValidator()
    {
        RuleFor(request => request.Kind).Must(value => Enum.TryParse<ExclusionKind>(value, out _));

        RuleFor(request => request.Pattern).NotEmpty()
            .When(request => request.Kind is nameof(ExclusionKind.Extension) or nameof(ExclusionKind.FileName)
                or nameof(ExclusionKind.Folder) or nameof(ExclusionKind.Contains));

        RuleFor(request => request.SizeBytes).NotNull()
            .When(request => request.Kind == nameof(ExclusionKind.Size))
            .WithMessage("Una exclusión por tamaño necesita un valor de tamaño.");

        RuleFor(request => request.AgeDays).NotNull()
            .When(request => request.Kind == nameof(ExclusionKind.Age))
            .WithMessage("Una exclusión por antigüedad necesita un valor de antigüedad.");

        RuleFor(request => request.Operator).NotEmpty()
            .When(request => request.Kind is nameof(ExclusionKind.Size) or nameof(ExclusionKind.Age));
    }
}

public sealed class FilterRequestValidator : AbstractValidator<FilterRequest>
{
    public FilterRequestValidator()
    {
        RuleFor(request => request.Pattern).NotEmpty().MaximumLength(500);
    }
}

public sealed class ScheduleRequestValidator : AbstractValidator<ScheduleRequest>
{
    public ScheduleRequestValidator()
    {
        RuleFor(request => request.Frequency).Must(value => Enum.TryParse<ScheduleFrequency>(value, out _));
        RuleFor(request => request.Times).NotEmpty().WithMessage("Hace falta al menos una hora de ejecución (RF-32).");
        RuleForEach(request => request.Times).Must(BeAValidTime).WithMessage("La hora debe tener el formato HH:mm.");
    }

    private static bool BeAValidTime(string value) => TimeOnly.TryParseExact(value, "HH:mm", out _);
}
