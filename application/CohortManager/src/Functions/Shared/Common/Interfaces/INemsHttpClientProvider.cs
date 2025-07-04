namespace Common;

using System.Security.Cryptography.X509Certificates;

public interface INemsHttpClientProvider
{
    HttpClient CreateClient(X509Certificate2? clientCertificate = null, bool bypassCertValidation = false);
}
