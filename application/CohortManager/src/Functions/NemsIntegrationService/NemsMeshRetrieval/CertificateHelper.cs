namespace NHS.CohortManager.CaasIntegrationService;

using System.Security.Cryptography.X509Certificates;

public static class CertificateHelper
{
    public static X509Certificate2Collection GetCertificatesFromString(string certificatesString)
    {
        X509Certificate2Collection certs = [];

        X509Certificate2[] pemCerts = certificatesString
            .Split("-----END CERTIFICATE-----", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pem => pem + "\n-----END CERTIFICATE-----")
            .Select(pem =>
            {
                var base64 = pem
                    .Replace("-----BEGIN CERTIFICATE-----", "")
                    .Replace("-----END CERTIFICATE-----", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Trim();

                return new X509Certificate2(Convert.FromBase64String(base64));
            })
            .ToArray();
        
        certs.AddRange(pemCerts);
        
        return certs;
    }
}