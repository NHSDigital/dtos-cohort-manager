namespace Common;

using System.Net;
using System.Text;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

public class CallFunction : ICallFunction
{

    private readonly ILogger<CallFunction> _logger;
    public CallFunction(ILogger<CallFunction> logger)
    {
        _logger = logger;
    }
    public async Task<HttpWebResponse> SendPost(string url, string postData)
    {
        return await GetHttpWebRequest(url, postData, "POST");
    }

    public async Task<HttpWebResponse> SendPut(string url, string postData)
    {
        return await GetHttpWebRequest(url, postData, "PUT");
    }

    public async Task<string> SendGet(string url)
    {
        return await GetAsync(url);
    }

    public async Task<string> SendGet(string url, Dictionary<string, string> parameters)
    {
        url = QueryHelpers.AddQueryString(url, parameters);
        return await GetAsync(url);
    }

    public async Task<bool> SendDelete(string url)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "DELETE";

        using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<string> GetResponseText(HttpWebResponse httpResponseData)
    {
        using (Stream stream = httpResponseData.GetResponseStream())
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                var responseText = await reader.ReadToEndAsync();
                return responseText;
            }
        }
    }

    //Private

    private async Task<string> GetAsync(string url)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);


        try
        {
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await GetResponseText(response);
                }
            }
        }
        catch (WebException wex)
        {
            _logger.LogError(wex, "Failed to execute web request to url: {url}, message: {message}",url,wex.Message);
            return await HandleWebException(wex);
        }

        return null;
    }



    private async Task<HttpWebResponse> GetHttpWebRequest(string url, string dataToSend, string Method)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        var data = Encoding.ASCII.GetBytes(dataToSend);
        request.Method = Method;
        request.Timeout = 999999999;
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = data.Length;

        using (var stream = await request.GetRequestStreamAsync())
        {
            await stream.WriteAsync(data, 0, data.Length);
        }

        try
        {
            var response = (HttpWebResponse)await request.GetResponseAsync();
            return response;
        }
        catch (WebException ex)
        {
            _logger.LogError(ex, "Failed to execute web request to url: {url}, message: {message}",url,ex.Message);
            return (HttpWebResponse)ex.Response;
        }

    }

    private async Task<string?> HandleWebException(WebException wex)
    {
        try
        {
            var response = (HttpWebResponse)wex.Response;
            if(response == null)
            {
                return null;
            }

            _logger.LogInformation("Web Exception Caught with response body. Http Reponse Code {Response Code}",response!.StatusCode);
            return await GetResponseText(response);
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex,"Couldn't Deserialize web exception response");
            return null;
        }
    }



}
