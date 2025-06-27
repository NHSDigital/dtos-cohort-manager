
namespace Common.Interfaces;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Extensions.Options;
using Model;

public class SendExceptionToHttp : IExceptionSender
{
    private IHttpClientFunction _httpClientFunction;

    private readonly HttpValidationConfig _httpValidationConfig;

    public SendExceptionToHttp(IHttpClientFunction httpClientFunction, IOptions<HttpValidationConfig> httpValidationConfig)
    {
        _httpClientFunction = httpClientFunction;
        _httpValidationConfig = httpValidationConfig.Value;
    }
    public async Task<bool> sendToCreateException(ValidationException validationException)
    {
        var response = await _httpClientFunction!.SendPost(_httpValidationConfig.ExceptionFunctionURL, JsonSerializer.Serialize(validationException));
        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
        {
            return false;
        }
        return true;
    }
}
