namespace Common;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Model;


public class HttpClientFunctionMock : IHttpClientFunction
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
        return JsonSerializer.Serialize(new ParticipantDemographic());
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
