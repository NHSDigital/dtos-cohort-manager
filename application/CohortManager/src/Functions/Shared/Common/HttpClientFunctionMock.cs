namespace Common;

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using System.Text.Json;

public class HttpClientFunctionMock : IHttpClientFunction
{

    public Task<HttpResponseMessage> SendPost(string url, string data)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendGet(string url)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendGet(string url, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> SendGetResponse(string url)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendGetOrThrowAsync(string url)
    {
        throw new NotImplementedException();
    }

    public async Task<HttpResponseMessage> SendPdsGet(string url, string bearerToken)
    {
        var HttpResponseData = new HttpResponseMessage();
        if (string.IsNullOrEmpty(url))
        {
            HttpResponseData.StatusCode = HttpStatusCode.InternalServerError;
            return HttpResponseData;
        }
        var Patient = new Patient();
        Patient.Id = "900000009";

        HttpResponseData.Content = new StringContent(JsonSerializer.Serialize(Patient));
        HttpResponseData.StatusCode = HttpStatusCode.OK;
        return HttpResponseData;
    }

    public Task<HttpResponseMessage> SendPut(string url, string data)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendDelete(string url)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetResponseText(HttpResponseMessage response)
    {
        throw new NotImplementedException();
    }
}
