using FluentAssertions;
using JMBackup.Application.Backup;
using Microsoft.Extensions.Time.Testing;

namespace JMBackup.Application.Tests.Backup;

public class StallDetectorTests
{
    [Fact]
    public void NoProgressReportedYet_BeforeThreshold_HasNotStalled()
    {
        var timeProvider = new FakeTimeProvider();
        var detector = new StallDetector(timeProvider);

        timeProvider.Advance(StallDetector.StallThreshold - TimeSpan.FromSeconds(1));

        detector.HasStalled.Should().BeFalse();
    }

    [Fact]
    public void NoProgressReportedYet_AfterThreshold_HasStalled()
    {
        var timeProvider = new FakeTimeProvider();
        var detector = new StallDetector(timeProvider);

        timeProvider.Advance(StallDetector.StallThreshold + TimeSpan.FromSeconds(1));

        detector.HasStalled.Should().BeTrue();
    }

    [Fact]
    public void ProgressAdvances_ResetsTheStallWindow()
    {
        var timeProvider = new FakeTimeProvider();
        var detector = new StallDetector(timeProvider);

        timeProvider.Advance(StallDetector.StallThreshold - TimeSpan.FromSeconds(1));
        detector.ReportProgress(1024);
        timeProvider.Advance(StallDetector.StallThreshold - TimeSpan.FromSeconds(1));

        detector.HasStalled.Should().BeFalse();
    }

    [Fact]
    public void ProgressReportedWithTheSameByteCount_DoesNotResetTheWindow()
    {
        var timeProvider = new FakeTimeProvider();
        var detector = new StallDetector(timeProvider);

        detector.ReportProgress(1024);
        timeProvider.Advance(StallDetector.StallThreshold - TimeSpan.FromSeconds(1));
        detector.ReportProgress(1024);
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        detector.HasStalled.Should().BeTrue();
    }

    [Fact]
    public void SlowButAdvancingProgress_NeverStalls()
    {
        var timeProvider = new FakeTimeProvider();
        var detector = new StallDetector(timeProvider);

        for (var i = 1; i <= 10; i++)
        {
            timeProvider.Advance(StallDetector.StallThreshold - TimeSpan.FromSeconds(1));
            detector.ReportProgress(i);
            detector.HasStalled.Should().BeFalse();
        }
    }
}
