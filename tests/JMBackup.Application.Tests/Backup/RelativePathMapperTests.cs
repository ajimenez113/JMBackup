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

    /// <summary>
    /// Regresión: las rutas de origen guardadas por el asistente tienen "\" nativo de
    /// Windows (p. ej. "C:\Users\ajimenez\Documentos"), no "/" — con eso, GetLeafName
    /// no encontraba ningún separador y devolvía la ruta absoluta entera como "nombre
    /// de hoja", rompiendo la ruta de destino de cada archivo copiado.
    /// </summary>
    [Fact]
    public void NotAbsolute_WindowsStyleSourceRoot_PrefixesWithTheSourceRootLeafNameOnly()
    {
        var result = RelativePathMapper.Map(@"C:\Users\ajimenez\Documentos", "archivo.txt", absolutePaths: false);

        result.Should().Be("Documentos/archivo.txt");
    }

    [Fact]
    public void Absolute_WindowsStyleSourceRoot_ReplicatesTheFullSourcePathWithoutTheDriveColon()
    {
        var result = RelativePathMapper.Map(@"C:\Users\ajimenez\Documentos", "archivo.txt", absolutePaths: true);

        result.Should().Be("C/Users/ajimenez/Documentos/archivo.txt");
    }
}
