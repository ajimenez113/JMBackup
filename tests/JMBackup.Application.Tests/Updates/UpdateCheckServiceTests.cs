using System.Text.Json;
using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using JMBackup.Application.Updates;
using JMBackup.Domain.Common;
using NSubstitute;

namespace JMBackup.Application.Tests.Updates;

public class UpdateCheckServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static (UpdateCheckService Service, ISettingsStore SettingsStore, IUpdateCheckClient Client) CreateService(string? updateCheckUrl)
    {
        var settingsStore = Substitute.For<ISettingsStore>();
        var generalSettingsJson = JsonSerializer.Serialize(new GeneralSettings { UpdateCheckUrl = updateCheckUrl }, JsonOptions);
        settingsStore.GetAsync("General", Arg.Any<CancellationToken>()).Returns(generalSettingsJson);

        var client = Substitute.For<IUpdateCheckClient>();
        var service = new UpdateCheckService(new SettingsService(settingsStore), client);
        return (service, settingsStore, client);
    }

    [Fact]
    public async Task CheckAsync_NoUrlConfigured_ReturnsNotConfiguredWithoutCallingTheClient()
    {
        var (service, _, client) = CreateService(updateCheckUrl: null);

        var result = await service.CheckAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Configured.Should().BeFalse();
        result.Value.UpdateAvailable.Should().BeNull();
        await client.DidNotReceive().FetchLatestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAsync_LatestVersionIsNewer_ReportsUpdateAvailable()
    {
        var (service, _, client) = CreateService("https://example.com/version.json");
        client.FetchLatestAsync("https://example.com/version.json", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UpdateCheckPayload("9.9.9", "https://example.com/download")));

        var result = await service.CheckAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Configured.Should().BeTrue();
        result.Value.UpdateAvailable.Should().BeTrue();
        result.Value.LatestVersion.Should().Be("9.9.9");
        result.Value.DownloadUrl.Should().Be("https://example.com/download");
    }

    [Fact]
    public async Task CheckAsync_LatestVersionIsNotNewer_ReportsNoUpdateAvailable()
    {
        var (service, _, client) = CreateService("https://example.com/version.json");
        client.FetchLatestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UpdateCheckPayload(ProductVersion.Current, null)));

        var result = await service.CheckAsync(CancellationToken.None);

        result.Value.UpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_ClientFails_PropagatesTheFailure()
    {
        var (service, _, client) = CreateService("https://example.com/version.json");
        var expectedError = new ResultError("Unreachable", "No se pudo contactar la URL.");
        client.FetchLatestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UpdateCheckPayload>(expectedError));

        var result = await service.CheckAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
    }

    [Fact]
    public async Task CheckAsync_PayloadVersionIsMalformed_ReturnsFailure()
    {
        var (service, _, client) = CreateService("https://example.com/version.json");
        client.FetchLatestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UpdateCheckPayload("not-a-version", null)));

        var result = await service.CheckAsync(CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("InvalidVersionFormat");
    }
}
