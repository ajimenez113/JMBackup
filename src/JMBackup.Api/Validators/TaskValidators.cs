using FluentValidation;
using JMBackup.Api.Contracts;
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

public sealed class TaskPathRequestValidator : AbstractValidator<TaskPathRequest>
{
    public TaskPathRequestValidator(Security.PathValidationService pathValidation)
    {
        RuleFor(request => request.Role).Must(value => Enum.TryParse<TaskPathRole>(value, out _));
        RuleFor(request => request.Path).Must(path => pathValidation.Validate(path).IsSuccess)
            .WithMessage("La ruta no es válida o contiene \"..\".");
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
