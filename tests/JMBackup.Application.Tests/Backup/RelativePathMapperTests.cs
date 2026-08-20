using FluentAssertions;
using JMBackup.Application.Backup;

namespace JMBackup.Application.Tests.Backup;

public class RelativePathMapperTests
{
    [Fact]
    public void NotAbsolute_PrefixesWithTheSourceRootLeafName()
    {
        var result = RelativePathMapper.Map("C:/Datos/Documentos", "sub/archivo.txt", absolutePaths: false);

        result.Should().Be("Documentos/sub/archivo.txt");
    }

    [Fact]
    public void NotAbsolute_RootFile_PrefixesWithTheLeafNameOnly()
    {
        var result = RelativePathMapper.Map("C:/Datos/Documentos", "archivo.txt", absolutePaths: false);

        result.Should().Be("Documentos/archivo.txt");
    }

    [Fact]
    public void Absolute_ReplicatesTheFullSourcePathWithoutTheDriveColon()
    {
        var result = RelativePathMapper.Map("C:/Datos/Documentos", "sub/archivo.txt", absolutePaths: true);

        result.Should().Be("C/Datos/Documentos/sub/archivo.txt");
    }

    [Fact]
    public void Absolute_UncRoot_StripsTheLeadingSlashes()
    {
        var result = RelativePathMapper.Map("//SERVIDOR/recurso", "archivo.txt", absolutePaths: true);

        result.Should().Be("SERVIDOR/recurso/archivo.txt");
    }
}
