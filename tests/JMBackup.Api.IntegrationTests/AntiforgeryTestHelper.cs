using System.Net;
using System.Text.Json;
using FluentAssertions;
using JMBackup.Api.Contracts;

namespace JMBackup.Api.IntegrationTests;

/// <summary>Pedir el token de antiforgery es el primer paso de cualquier prueba que escriba (POST/PUT/PATCH/DELETE).</summary>
internal static class AntiforgeryTestHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync(new Uri("/api/antiforgery/token", UriKind.Relative));
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, text);
        var body = JsonSerializer.Deserialize<AntiforgeryTokenResponse>(text, JsonOptions);
        return body!.Token;
    }
}
