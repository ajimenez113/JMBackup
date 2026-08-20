using FluentAssertions;
using JMBackup.Storage.Local;

namespace JMBackup.Storage.Tests.Local;

public class LocalPathTranslatorTests
{
    [Fact]
    public void ToNative_EmptyRelativePath_ReturnsTheRootUnchanged()
    {
        var result = LocalPathTranslator.ToNative(@"C:\Datos", string.Empty);

        result.Should().Be(@"C:\Datos");
    }

    [Fact]
    public void ToNative_ConvertsForwardSlashesToBackslashes()
    {
        var result = LocalPathTranslator.ToNative(@"C:\Datos", "sub/carpeta/archivo.txt");

        result.Should().Be(@"C:\Datos\sub\carpeta\archivo.txt");
    }

    [Fact]
    public void ToNormalized_ConvertsBackslashesToForwardSlashes()
    {
        var result = LocalPathTranslator.ToNormalized(@"sub\carpeta\archivo.txt");

        result.Should().Be("sub/carpeta/archivo.txt");
    }

    [Fact]
    public void GetRelativeNormalized_ReturnsThePathRelativeToTheRootWithForwardSlashes()
    {
        var result = LocalPathTranslator.GetRelativeNormalized(@"C:\Datos", @"C:\Datos\sub\archivo.txt");

        result.Should().Be("sub/archivo.txt");
    }
}
