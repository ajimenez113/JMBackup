using JMBackup.Api.Contracts;
using JMBackup.Api.Validators;
using JMBackup.Application.Abstractions;

namespace JMBackup.Api.Endpoints;

/// <summary>Credenciales de red guardadas (ADR-008), para que las rutas UNC de una tarea las referencien por Id.</summary>
public static class CredentialEndpoints
{
    public static void MapCredentialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/credentials").WithTags("Credentials").RequireAuthorization();

        group.MapGet(string.Empty, async (ICredentialRepository repository, CancellationToken cancellationToken) =>
            Results.Ok((await repository.ListAsync(cancellationToken).ConfigureAwait(false)).Select(c => c.ToResponse())))
            .Produces<IEnumerable<CredentialResponse>>();

        group.MapPost(string.Empty, async (
            CredentialRequest request, ICredentialRepository repository, INetworkCredentialProtector protector, CancellationToken cancellationToken) =>
        {
            var entity = request.ToEntity(protector);
            var id = await repository.CreateAsync(entity, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/credentials/{id}", entity.ToResponse());
        }).WithValidation<CredentialRequest>().Produces<CredentialResponse>(StatusCodes.Status201Created);

        group.MapDelete("/{id:int}", async (int id, ICredentialRepository repository, CancellationToken cancellationToken) =>
        {
            await repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);
    }
}
