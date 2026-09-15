using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JMBackup.Api.Contracts;
using static JMBackup.Api.IntegrationTests.AntiforgeryTestHelper;

namespace JMBackup.Api.IntegrationTests;

/// <summary>
/// Antes de esta suite, TaskPathEndpoints forzaba BackendType.Local sin importar lo que
/// mandara el pedido — FTP y S3 quedaban inalcanzables desde la API real aunque sus
/// backends estuvieran completos y probados (fase 5, hito 2). Estas pruebas ejercitan
/// el pipeline real (validación + mapeo + persistencia), no solo compilación.
/// </summary>
[Collection(ApiIntegrationTestGroup.Name)]
public sealed class TaskPathEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public TaskPathEndpointsTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AddPath_WithS3Backend_RoundTripsAllTheS3SpecificFields()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        var task = await CreateTaskAsync(client, antiforgeryToken, "Tarea con destino S3");
        var credential = await CreateCredentialAsync(client, antiforgeryToken, "AWS", "S3", "AKIA-test", "secreto");

        var response = await PostPathAsync(client, antiforgeryToken, task!.Id, new TaskPathRequest(
            "Destination", "S3", "mi-bucket/prefijo", credential!.Id, Position: 0,
            Encrypted: false, Region: "us-east-1", StorageClass: "GLACIER", ServerSideEncryption: true));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskPathResponse>();
        created.Should().NotBeNull();
        created!.BackendType.Should().Be("S3");
        created.Region.Should().Be("us-east-1");
        created.StorageClass.Should().Be("GLACIER");
        created.ServerSideEncryption.Should().BeTrue();
        created.CredentialId.Should().Be(credential.Id);
    }

    [Fact]
    public async Task AddPath_WithFtpBackend_RoundTripsTheEncryptedFlag()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        var task = await CreateTaskAsync(client, antiforgeryToken, "Tarea con destino FTP");
        var credential = await CreateCredentialAsync(client, antiforgeryToken, "FTP1", "Ftp", "usuario", "secreto");

        var response = await PostPathAsync(client, antiforgeryToken, task!.Id, new TaskPathRequest(
            "Destination", "Ftp", "ftp.ejemplo.com:21/respaldos", credential!.Id, Position: 0,
            Encrypted: true, Region: null, StorageClass: null, ServerSideEncryption: false));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskPathResponse>();
        created.Should().NotBeNull();
        created!.BackendType.Should().Be("Ftp");
        created.Encrypted.Should().BeTrue();
    }

    [Fact]
    public async Task AddPath_WithSftpBackend_IsRejected()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        var task = await CreateTaskAsync(client, antiforgeryToken, "Tarea con destino SFTP");

        var response = await PostPathAsync(client, antiforgeryToken, task!.Id, new TaskPathRequest(
            "Destination", "Sftp", "sftp.ejemplo.com/respaldos", null, Position: 0,
            Encrypted: false, Region: null, StorageClass: null, ServerSideEncryption: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPath_S3WithoutRegion_IsRejected()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        var task = await CreateTaskAsync(client, antiforgeryToken, "Tarea S3 sin región");
        var credential = await CreateCredentialAsync(client, antiforgeryToken, "AWS2", "S3", "AKIA-test2", "secreto");

        var response = await PostPathAsync(client, antiforgeryToken, task!.Id, new TaskPathRequest(
            "Destination", "S3", "mi-bucket", credential!.Id, Position: 0,
            Encrypted: false, Region: null, StorageClass: null, ServerSideEncryption: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPath_FtpWithoutCredential_IsRejected()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        var task = await CreateTaskAsync(client, antiforgeryToken, "Tarea FTP sin credencial");

        var response = await PostPathAsync(client, antiforgeryToken, task!.Id, new TaskPathRequest(
            "Destination", "Ftp", "ftp.ejemplo.com/respaldos", null, Position: 0,
            Encrypted: false, Region: null, StorageClass: null, ServerSideEncryption: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPath_WithACredentialIdThatDoesNotExist_IsRejected()
    {
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        var task = await CreateTaskAsync(client, antiforgeryToken, "Tarea con credencial inexistente");

        var response = await PostPathAsync(client, antiforgeryToken, task!.Id, new TaskPathRequest(
            "Destination", "Ftp", "ftp.ejemplo.com/respaldos", 999_999, Position: 0,
            Encrypted: false, Region: null, StorageClass: null, ServerSideEncryption: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<HttpResponseMessage> PostPathAsync(HttpClient client, string antiforgeryToken, int taskId, TaskPathRequest request)
    {
        using var pathRequest = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/tasks/{taskId}/paths", UriKind.Relative))
        {
            Content = JsonContent.Create(request),
        };
        pathRequest.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);
        return await client.SendAsync(pathRequest);
    }

    private static async Task<CredentialResponse?> CreateCredentialAsync(
        HttpClient client, string antiforgeryToken, string alias, string backendType, string username, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/credentials", UriKind.Relative))
        {
            Content = JsonContent.Create(new CredentialRequest(alias, backendType, username, password)),
        };
        request.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<CredentialResponse>();
    }

    private static async Task<TaskResponse?> CreateTaskAsync(HttpClient client, string antiforgeryToken, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/tasks", UriKind.Relative))
        {
            Content = JsonContent.Create(new CreateTaskRequest(
                name, GroupId: null, Enabled: true, Mode: "Incremental", OrderStrategy: "NameAscending",
                IncludeSubfolders: true, AbsolutePaths: false, RemoveEmptyDirs: false, VerifyLevel: "SizeOnly")),
        };
        request.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<TaskResponse>();
    }
}
