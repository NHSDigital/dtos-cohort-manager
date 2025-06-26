
namespace Common.Interfaces;

using System.Net;
using System.Text.Json;
using Common;
using Model;

public class SendExceptionToHttp : IExceptionSender
{
    public IHttpClientFunction _httpClientFunction;

    public SendExceptionToHttp(IHttpClientFunction httpClientFunction)
    {
        _httpClientFunction = httpClientFunction;
    }
    public async Task<bool> sendToCreateException(ValidationException validationException, string createExceptionUrl)
    {
        var response = await _httpClientFunction!.SendPost(createExceptionUrl, JsonSerializer.Serialize(validationException));
        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
        {
            return false;
        }
        return true;
    }
}