namespace Common;

using System.Net;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class HttpParserHelper : IHttpParserHelper
{
    private readonly ILogger<HttpParserHelper> _logger;
    private readonly ICreateResponse _createResponse;
    public HttpParserHelper(ILogger<HttpParserHelper> logger, ICreateResponse createResponse)
    {
        _logger = logger;
        _createResponse = createResponse;
    }
    public int GetQueryParameterAsInt(HttpRequestData req, string key)
    {
        var defaultValue = 0;
        var queryString = req.Query[key];
        return int.TryParse(queryString, out int value) ? value : defaultValue;
    }
    public HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage)
    {
        _logger.LogError(errorMessage);
        return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, errorMessage);
    }

    public bool GetQueryParameterAsBool(HttpRequestData req, string key, bool defaultValue = false)
    {
        var queryString = req.Query[key];

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return defaultValue;
        }

        switch (queryString.ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "y":
                return true;
            case "false":
            case "0":
            case "no":
            case "n":
                return false;
            default:
                return defaultValue;
        }
    }
}
