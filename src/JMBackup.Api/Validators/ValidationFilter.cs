using FluentValidation;

namespace JMBackup.Api.Validators;

/// <summary>Corre el <see cref="IValidator{T}"/> registrado para <typeparamref name="TRequest"/> antes del handler (CLAUDE.md §5.1: FluentValidation en el borde).</summary>
public static class ValidationFilter
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
            var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

            if (validator is not null && request is not null)
            {
                var result = await validator.ValidateAsync(request).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    return Results.ValidationProblem(result.ToDictionary());
                }
            }

            return await next(context).ConfigureAwait(false);
        });
}
