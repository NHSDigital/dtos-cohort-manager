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
        var path = FindInParents("nems_certificate.pem");
        Assert.IsTrue(File.Exists(path), $"Test certificate not found at {path}");
        var certs = await MeshMailboxExtension.GetCACertificates(path, null);
        Assert.IsNotNull(certs);
        Assert.IsInstanceOfType(certs, typeof(X509Certificate2Collection));
        Assert.IsTrue(certs!.Count > 0);
    }

    [TestMethod]
    public async Task GetCACertificates_NoInputs_ReturnsNull()
    {
        var certs = await MeshMailboxExtension.GetCACertificates(null, null);
        Assert.IsNull(certs);
    }
    private static string FindInParents(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return fileName; // will fail later if not found
    }
}
