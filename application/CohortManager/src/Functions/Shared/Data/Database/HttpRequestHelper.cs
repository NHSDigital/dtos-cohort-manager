namespace Data.Database;

using System.Net;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
public class HttpRequestHelper
{
    private readonly ILogger<HttpRequestHelper> _logger;
    private readonly ICreateResponse _createResponse;
    public HttpRequestHelper(ILogger<HttpRequestHelper> logger, ICreateResponse createResponse)
    {
        _logger = logger;
        _createResponse = createResponse;
    }
    public static int GetQueryParameterAsInt(HttpRequestData req, string key, int defaultValue = 0)
    {
        var queryString = req.Query[key];
        return int.TryParse(queryString, out int value) ? value : defaultValue;
    }

    public static int GetRowCount(HttpRequestData req)
    {
        return GetQueryParameterAsInt(req, "rowCount");
    }

    public static int GetServiceProviderId(HttpRequestData req)
    {
        return GetQueryParameterAsInt(req, "serviceProviderId");
    }

    public HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage)
    {
        _logger.LogError(errorMessage);
        return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, errorMessage);
    }
}
