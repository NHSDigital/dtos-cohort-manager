namespace Common;

using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Helpers for parsing certificates from PEM content.
/// </summary>
public static class CertificateHelper
{
    /// <summary>
    /// Parses one or more PEM-encoded certificates from a string into a collection.
    /// </summary>
    /// <param name="certificatesString">A string containing one or more concatenated PEM certificates.</param>
    /// <returns>An <see cref="X509Certificate2Collection"/> containing parsed certificates.</returns>
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
