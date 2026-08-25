using System.Net;
using FluentAssertions;

namespace JMBackup.Api.IntegrationTests;

[Collection(ApiIntegrationTestGroup.Name)]
public class SmokeTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Host_StartsAndAppliesMigrations()
    {
        using var client = factory.CreateClient();

        // Un endpoint real y anónimo confirma que Kestrel, el enrutamiento y el
        // arranque de JMBackupDbContext (migraciones incluidas) funcionaron sin excepción.
        var response = await client.GetAsync(new Uri("/api/auth/session", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
