namespace Common;

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Model;
using Common.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Mock implementation of IHttpClientFunction specifically designed for PDS (Personal Demographics Service) calls.
/// This mock returns PdsDemographic objects for SendGet calls and FHIR Patient JSON for SendPdsGet calls.
///
/// WARNING: This is NOT a general-purpose HTTP client mock. It is designed specifically for PDS service testing.
/// Other services (NEMS, ServiceNow, etc.) should not use this mock as it returns PDS-specific data structures.
/// </summary>
public class PdsHttpClientMock : HttpClientFunction
{

    private readonly ILogger<PdsHttpClientMock> _logger;

    public PdsHttpClientMock(ILogger<HttpClientFunction> httpLogger, IHttpClientFactory factory, ILogger<PdsHttpClientMock> logger)
        : base(httpLogger, factory)
    {
        _logger = logger;
    }

    public override async Task<HttpResponseMessage> SendPdsGet(string url, string bearerToken)
    {
        var address = new Uri(url);

        if (address.Host != "sandbox.api.service.nhs.uk")
        {
            return await base.SendPdsGet(url, bearerToken);
        }

        var nhsNumber = address.Segments.Last().TrimEnd('/');
        var patient = await GetPatientMockObject(nhsNumber);

        if (patient == null)
        {
            string? notFoundResponseBody;
            if (nhsNumber == "9111231130")
            {
                notFoundResponseBody = await File.ReadAllTextAsync("MockedPDSData/patient-not-found-invalid-resource.json");
            }
            else
            {
                notFoundResponseBody = await File.ReadAllTextAsync("MockedPDSData/patient-not-found.json");
            }
            return HttpStubUtilities.CreateFakeHttpResponse(url, notFoundResponseBody, HttpStatusCode.NotFound);
        }
        return HttpStubUtilities.CreateFakeHttpResponse(url, patient);



    }

    private async Task<string?> GetPatientMockObject(string? nhsNumber = null)
    {

        string path = nhsNumber is null ? "MockedPDSData/complete-patient.json" : $"MockedPDSData/complete-patient-{nhsNumber}.json";
        if (!File.Exists(path))
        {
            _logger.LogWarning("Mocked PDS Data file couldn't be found");
            return null;
        }
        return await File.ReadAllTextAsync(path);

    }

}
