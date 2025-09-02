namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Security.Cryptography.X509Certificates;
using System.IO;
using System;
using Common;

[TestClass]
public class MeshMailboxExtensionTests
{
    [TestMethod]
    public async Task GetCACertificates_FromFilePath_ReturnsCollection()
    {
        // Create a temporary self-signed certificate and write as PEM to a temp file
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var der = cert.Export(X509ContentType.Cert);
        var pem = "-----BEGIN CERTIFICATE-----\n"
                + Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks)
                + "\n-----END CERTIFICATE-----\n";

        var tempPath = Path.Combine(Path.GetTempPath(), $"test-cert-{Guid.NewGuid():N}.pem");
        await File.WriteAllTextAsync(tempPath, pem);

        try
        {
            var certs = await MeshMailboxExtension.GetCACertificates(tempPath, null);
            Assert.IsNotNull(certs);
            Assert.IsInstanceOfType(certs, typeof(X509Certificate2Collection));
            Assert.IsTrue(certs!.Count > 0);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [TestMethod]
    public async Task GetCACertificates_NoInputs_ReturnsNull()
    {
        var certs = await MeshMailboxExtension.GetCACertificates(null, null);
        Assert.IsNull(certs);
    }
    // kept for potential future use; not used after temp-cert approach
    private static string FindInParents(string fileName) => fileName;
}
