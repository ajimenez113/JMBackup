using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JMBackup.Api.Contracts;
using static JMBackup.Api.IntegrationTests.AntiforgeryTestHelper;

namespace JMBackup.Api.IntegrationTests;

[Collection(ApiIntegrationTestGroup.Name)]
public sealed class ScheduleEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public ScheduleEndpointsTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AddSchedule_ReschedulesTheTaskInQuartz_WithoutThrowing()
    {
        // Regresión: QuartzTaskScheduler.RescheduleAsync guardaba el TaskId como int en
        // el JobDataMap, pero UseProperties = true exige que todo valor sea string —
        // tiraba JobPersistenceException recién al guardar, nunca detectado porque
        // Quartz nunca se había probado de punta a punta hasta esta prueba.
        using var client = _factory.CreateSecureClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        var task = await CreateTaskAsync(client, antiforgeryToken, "Tarea con horario");

        using var scheduleRequest = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/tasks/{task!.Id}/schedules", UriKind.Relative))
        {
            Content = JsonContent.Create(new ScheduleRequest("Weekly", [1, 3, 5], null, ["09:00", "18:00"])),
        };
        scheduleRequest.Headers.Add("X-XSRF-TOKEN", antiforgeryToken);

        var response = await client.SendAsync(scheduleRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
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
