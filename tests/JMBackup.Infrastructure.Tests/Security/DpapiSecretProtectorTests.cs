using FluentAssertions;
using JMBackup.Infrastructure.Security;

namespace JMBackup.Infrastructure.Tests.Security;

public class DpapiSecretProtectorTests
{
    [Fact]
    public void Protect_ThenUnprotect_RoundTripsTheOriginalText()
    {
        var protector = new DpapiSecretProtector();

        var protectedBytes = protector.Protect("contraseña de prueba con ñ y acentos áéí");
        var result = protector.Unprotect(protectedBytes);

        result.Should().Be("contraseña de prueba con ñ y acentos áéí");
    }

    [Fact]
    public void Protect_NeverProducesThePlainTextBytes()
    {
        var protector = new DpapiSecretProtector();
        var plainText = "S3cr3t0!";

        var protectedBytes = protector.Protect(plainText);

        System.Text.Encoding.UTF8.GetBytes(plainText).Should().NotBeEquivalentTo(protectedBytes);
    }

    [Fact]
    public void Protect_TheSamePlainTextTwice_ProducesDifferentBlobs()
    {
        // DPAPI mezcla un vector aleatorio en cada llamada: dos cifrados del mismo
        // secreto no deben ser iguales byte a byte.
        var protector = new DpapiSecretProtector();

        var first = protector.Protect("misma-contraseña");
        var second = protector.Protect("misma-contraseña");

        first.Should().NotBeEquivalentTo(second);
    }

    [Fact]
    public void Unprotect_TamperedBlob_Throws()
    {
        var protector = new DpapiSecretProtector();
        var protectedBytes = protector.Protect("contraseña");
        protectedBytes[^1] ^= 0xFF;

        var act = () => protector.Unprotect(protectedBytes);

        act.Should().Throw<System.Security.Cryptography.CryptographicException>();
    }
}
