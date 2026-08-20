using FluentAssertions;
using JMBackup.Application.Backup;

namespace JMBackup.Application.Tests.Backup;

public class PauseControllerTests
{
    [Fact]
    public async Task WaitIfPausedAsync_NotPaused_ReturnsImmediately()
    {
        using var controller = new PauseController();

        var wait = controller.WaitIfPausedAsync(CancellationToken.None);
        var completed = await Task.WhenAny(wait, Task.Delay(TimeSpan.FromSeconds(1)));

        completed.Should().BeSameAs(wait);
    }

    [Fact]
    public async Task WaitIfPausedAsync_Paused_BlocksUntilResume()
    {
        using var controller = new PauseController();
        controller.Pause();

        var wait = controller.WaitIfPausedAsync(CancellationToken.None);
        var raceWithTimeout = await Task.WhenAny(wait, Task.Delay(TimeSpan.FromMilliseconds(200)));
        raceWithTimeout.Should().NotBeSameAs(wait, "todavía está en pausa");

        controller.Resume();
        var completed = await Task.WhenAny(wait, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.Should().BeSameAs(wait);
    }

    [Fact]
    public void PauseTwice_IsIdempotent()
    {
        using var controller = new PauseController();

        controller.Pause();
        var act = controller.Pause;

        act.Should().NotThrow();
        controller.IsPaused.Should().BeTrue();
    }

    [Fact]
    public void ResumeWithoutPause_IsANoOp()
    {
        using var controller = new PauseController();

        var act = controller.Resume;

        act.Should().NotThrow();
        controller.IsPaused.Should().BeFalse();
    }
}
