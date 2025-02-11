namespace DataServices.Core;

using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;

internal static class HttpHelpers
{
    internal static HttpResponseData CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode)
    {
        var errorResponse = new DataServiceResponse<string> { ErrorMessage = message };
        return CreateHttpResponse(req, errorResponse, statusCode);
    }
    internal static HttpResponseData CreateHttpResponse(HttpRequestData req, DataServiceResponse<string> dataServiceResponse, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
    {
        HttpStatusCode statusCode;
        byte[] responseBody = null!;
        if(httpStatusCode == HttpStatusCode.NoContent){
            responseBody = Encoding.UTF8.GetBytes("");
            statusCode = httpStatusCode;
        }
        else if (dataServiceResponse.ErrorMessage == null)
        {
            statusCode = HttpStatusCode.OK;
            responseBody = Encoding.UTF8.GetBytes(dataServiceResponse.JsonData);
        }
        else if (dataServiceResponse.ErrorMessage != null)
        {
            responseBody = Encoding.UTF8.GetBytes(dataServiceResponse.ErrorMessage);
            statusCode = httpStatusCode;
        }
        else if (string.IsNullOrWhiteSpace(dataServiceResponse.JsonData))
        {
            responseBody = Encoding.UTF8.GetBytes("");
            statusCode = HttpStatusCode.NoContent;
        }
        else
        {
            responseBody = Encoding.UTF8.GetBytes(dataServiceResponse.ErrorMessage);
            statusCode = httpStatusCode;
        }

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");


        response.Body = new MemoryStream(responseBody);
        return response;
    }

    internal static bool GetBooleanQueryItem(HttpRequestData req, string headerKey, bool defaultValue = false)
    {
        if(req.Query[headerKey] == null){
            return defaultValue;
        }
        if(bool.TryParse(req.Query[headerKey],out var result)){
            return result;
        }
        return defaultValue;
    }
}
