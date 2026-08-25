using FluentAssertions;
using JMBackup.Storage.Remote;

namespace JMBackup.Storage.Tests.Remote;

public sealed class ResumeDecisionTests
{
    private static readonly DateTimeOffset RegisteredModifiedUtc =
        DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void ShouldResume_NoExistingPartial_ReturnsFalse()
    {
        var result = ResumeDecision.ShouldResume(existingPartial: null, requestedSize: 100, RegisteredModifiedUtc);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldResume_SameSizeAndModifiedDate_ReturnsTrue()
    {
        var partial = new PartialUpload(Size: 100, RegisteredModifiedUtc, BytesTransferred: 40);

        var result = ResumeDecision.ShouldResume(partial, requestedSize: 100, RegisteredModifiedUtc);

        result.Should().BeTrue();
    }

    /// <summary>
    /// El caso de seguridad que pidió el usuario explícitamente: el origen cambió de
    /// tamaño entre el intento que dejó el parcial y este — no se reanuda, aunque la
    /// fecha de modificación coincida. Reanudar acá produciría un archivo corrupto
    /// reportado como éxito.
    /// </summary>
    [Fact]
    public void ShouldResume_SourceSizeChangedSinceThePartialWasCreated_ReturnsFalse()
    {
        var partial = new PartialUpload(Size: 100, RegisteredModifiedUtc, BytesTransferred: 40);

        var result = ResumeDecision.ShouldResume(partial, requestedSize: 250, RegisteredModifiedUtc);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Mismo caso de seguridad, pero con la fecha de modificación distinta y el
    /// tamaño igual — un archivo puede cambiar de contenido sin cambiar de tamaño.
    /// </summary>
    [Fact]
    public void ShouldResume_SourceModifiedDateChangedSinceThePartialWasCreated_ReturnsFalse()
    {
        var partial = new PartialUpload(Size: 100, RegisteredModifiedUtc, BytesTransferred: 40);
        var laterModifiedUtc = RegisteredModifiedUtc.AddMinutes(5);

        var result = ResumeDecision.ShouldResume(partial, requestedSize: 100, laterModifiedUtc);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldResume_BothSizeAndModifiedDateChanged_ReturnsFalse()
    {
        var partial = new PartialUpload(Size: 100, RegisteredModifiedUtc, BytesTransferred: 40);
        var laterModifiedUtc = RegisteredModifiedUtc.AddMinutes(5);

        var result = ResumeDecision.ShouldResume(partial, requestedSize: 250, laterModifiedUtc);

        result.Should().BeFalse();
    }
}
