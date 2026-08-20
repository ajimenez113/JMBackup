namespace JMBackup.Api.IntegrationTests;

/// <summary>
/// <see cref="ApiWebApplicationFactory"/> apunta <c>Program.cs</c> a una carpeta de
/// datos temporal mediante la variable de entorno <c>Paths__DataDirectory</c>, que es
/// global al proceso. xUnit corre en paralelo las clases de prueba de colecciones
/// distintas por defecto, así que sin esta colección compartida dos fábricas de
/// distintas clases podrían pisarse el valor entre sí. Todas las clases de este
/// proyecto deben declarar <c>[Collection(Name)]</c>.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiIntegrationTestGroup : ICollectionFixture<ApiWebApplicationFactory>
{
    public const string Name = "ApiIntegrationTests";
}
