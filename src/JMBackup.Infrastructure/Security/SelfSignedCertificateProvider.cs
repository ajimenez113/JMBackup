using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using JMBackup.Infrastructure.Options;

namespace JMBackup.Infrastructure.Security;

/// <summary>
/// Certificado autofirmado con SAN de hostname e IP, generado al primer arranque
/// (RF-113). Se guarda en <c>jmbackup.pfx</c> dentro del directorio de datos y se
/// reutiliza en arranques siguientes. Instalarlo en el almacén de confianza de
/// Windows es una acción interactiva que le corresponde a la interfaz (fase 3), no a
/// esta clase.
/// </summary>
public sealed class SelfSignedCertificateProvider(JMBackupPathsOptions paths)
{
    private const string FileName = "jmbackup.pfx";

    public X509Certificate2 GetOrCreateCertificate()
    {
        var certificatePath = Path.Combine(paths.DataDirectory, FileName);

        if (File.Exists(certificatePath))
        {
            return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password: null, X509KeyStorageFlags.Exportable);
        }

        using var certificate = CreateSelfSignedCertificate();
        var exported = certificate.Export(X509ContentType.Pfx);

        Directory.CreateDirectory(paths.DataDirectory);
        File.WriteAllBytes(certificatePath, exported);

        return X509CertificateLoader.LoadPkcs12(exported, password: null, X509KeyStorageFlags.Exportable);
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={Environment.MachineName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(Environment.MachineName);
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);

        foreach (var address in GetLocalIPv4Addresses())
        {
            sanBuilder.AddIpAddress(address);
        }

        request.CertificateExtensions.Add(sanBuilder.Build());

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddYears(5);

        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private static List<IPAddress> GetLocalIPv4Addresses()
    {
        try
        {
            return Dns.GetHostAddresses(Dns.GetHostName())
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                .ToList();
        }
        catch (SocketException)
        {
            return [];
        }
    }
}
