namespace Common.Interfaces;

using Microsoft.Azure.Functions.Worker.Http;

public interface IHttpParserHelper
{
    HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage);
    int GetQueryParameterAsInt(HttpRequestData req, string key, int defaultValue = 0);
    bool GetQueryParameterAsBool(HttpRequestData req, string key, bool defaultValue = false);
    DateTime? GetQueryParameterAsDateTime(HttpRequestData req, string key);
};
