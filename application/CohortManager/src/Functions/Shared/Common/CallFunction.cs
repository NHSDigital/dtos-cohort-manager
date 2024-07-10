namespace Common;

using System.Net;
using System.Text;

public class CallFunction : ICallFunction
{
    public async Task<HttpWebResponse> SendPost(string url, string postData)
    {
        return await GetHttpWebRequest(url, postData, "POST");
    }

    public async Task<string> SendGet(string url)
    {
        return await GetAsync(url);
    }

    private async Task<string> GetAsync(string uri)
    {
        var request = (HttpWebRequest)WebRequest.Create(uri);

        using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await GetResponseText(response);
            }
        }

        return null;
    }

    private async Task<HttpWebResponse> GetHttpWebRequest(string url, string dataToSend, string Method)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);
        var data = Encoding.ASCII.GetBytes(dataToSend);
        request.Method = Method;
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = data.Length;

        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        var response = (HttpWebResponse)await request.GetResponseAsync();

        return response;
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
}
