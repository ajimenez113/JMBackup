using System.Net;
using FluentAssertions;

namespace JMBackup.Api.IntegrationTests;

public class SmokeTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Host_StartsAndAppliesMigrations()
    {
        using var client = factory.CreateClient();

        // Todavía no hay endpoints (llegan en la fase 2): un 404 confirma que Kestrel,
        // el enrutamiento y el arranque de JMBackupDbContext funcionaron sin excepción.
        var response = await client.GetAsync(new Uri("/", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
