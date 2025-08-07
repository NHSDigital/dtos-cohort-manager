namespace Common;

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Model;

/// <summary>
/// Mock implementation of IHttpClientFunction specifically designed for PDS (Personal Demographics Service) calls.
/// This mock returns PdsDemographic objects for SendGet calls and FHIR Patient JSON for SendPdsGet calls.
///
/// WARNING: This is NOT a general-purpose HTTP client mock. It is designed specifically for PDS service testing.
/// Other services (NEMS, ServiceNow, etc.) should not use this mock as it returns PDS-specific data structures.
/// </summary>
public class PdsHttpClientMock : IHttpClientFunction
{
    public async Task<HttpResponseMessage> SendPost(string url, string data)
    {
        await Task.CompletedTask;
        return HttpStubUtilities.CreateFakeHttpResponse(url,"");
    }
    public Task<HttpResponseMessage> SendPost(string url, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<string> SendGet(string url, Dictionary<string, string> parameters)
    {
        await Task.CompletedTask;
        return JsonSerializer.Serialize(new PdsDemographic());
    }

    public async Task<string> SendGet(string url)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public async Task<string> SendGetOrThrowAsync(string url)
    {
        await Task.CompletedTask;
        return "";
    }

    public async Task<HttpResponseMessage> SendPdsGet(string url, string bearerToken)
    {
        var patient = GetPatientMockObject("complete-patient.json");
        await Task.CompletedTask;
        return HttpStubUtilities.CreateFakeHttpResponse(url, patient);
    }

    private static string GetPatientMockObject(string filename)
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var filePath = Path.Combine(currentDirectory, filename);

        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        string keyContent = File.ReadAllText(filePath);
        return keyContent;
    }

    public async Task<HttpResponseMessage> SendPut(string url, string data)
    {
        await Task.CompletedTask;
        return HttpStubUtilities.CreateFakeHttpResponse(url,"");
    }

    public async Task<bool> SendDelete(string url)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public async Task<string> GetResponseText(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<HttpResponseMessage> SendGetResponse(string url)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

}
