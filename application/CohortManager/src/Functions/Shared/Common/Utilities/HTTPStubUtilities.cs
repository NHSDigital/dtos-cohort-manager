namespace Common;

using System.Net;

public static class HTTPStubUtilities
{

    /// <summary>
    /// takes in a fake string content and returns 200 OK response
    /// </summary>
    /// <param name="url"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static HttpResponseMessage CreateFakeHttpResponse(string url, string content = "")
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
    /// <summary>
    /// takes in a fake string content and returns 200 OK response or a Given Response
    /// </summary>
    /// <param name="url"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static HttpResponseMessage CreateFakeHttpResponse(string url, string content = "", HttpStatusCode httpStatusCode = HttpStatusCode.OK, Uri? location = null)
    {
        var HttpResponseData = new HttpResponseMessage();
        if (string.IsNullOrEmpty(url))
        {
            HttpResponseData.StatusCode = HttpStatusCode.InternalServerError;
            return HttpResponseData;
        }
        HttpResponseData.Headers.Location = location;
        HttpResponseData.Content = new StringContent(content);
        HttpResponseData.StatusCode = HttpStatusCode.OK;
        return HttpResponseData;
    }

}
