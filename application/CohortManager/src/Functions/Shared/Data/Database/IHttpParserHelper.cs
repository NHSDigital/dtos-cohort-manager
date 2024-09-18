namespace Data.Database;

using Microsoft.Azure.Functions.Worker.Http;


public interface IHttpParserHelper
{
    public HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage);
    public int GetRowCount(HttpRequestData req);
    public int GetServiceProviderId(HttpRequestData req);
    public int GetQueryParameterAsInt(HttpRequestData req, string key, int defaultValue = 0);

};
