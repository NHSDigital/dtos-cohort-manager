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
        return CreateFakeHttpResponse(url);
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
        return CreateFakeHttpResponse(url, patient);
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
        return CreateFakeHttpResponse(url);
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


    /// <summary>
    /// takes in a fake string content and returns 200 OK response 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    private static HttpResponseMessage CreateFakeHttpResponse(string url, string content = "")
    {
        var HttpResponseData = new HttpResponseMessage();
        if (string.IsNullOrEmpty(url))
        {
            HttpResponseData.StatusCode = HttpStatusCode.InternalServerError;
            return HttpResponseData;
        }

        HttpResponseData.Content = new StringContent(content);
        HttpResponseData.StatusCode = HttpStatusCode.OK;
        return HttpResponseData;
    }

    public Task<HttpResponseMessage> GetPDSRecord(string url, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
}
