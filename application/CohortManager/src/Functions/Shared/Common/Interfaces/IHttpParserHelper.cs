namespace Common.Interfaces;

using Microsoft.Azure.Functions.Worker.Http;

public interface IHttpParserHelper
{
    HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage);
    int GetRowCount(HttpRequestData req);
    int GetScreeningServiceId(HttpRequestData req);
    int GetQueryParameterAsInt(HttpRequestData req, string key);
    bool GetQueryParameterAsBool(HttpRequestData req, string key, bool defaultValue = false);
};
