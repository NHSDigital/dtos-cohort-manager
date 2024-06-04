namespace Common;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;


public class CallFunction : ICallFunction
{
    public async Task<HttpWebResponse> SendPost(string url, string postData)
    {
        return await GetHttpWebRequest(url, postData, "POST");
    }

    public async Task<HttpWebResponse> SendGet(string url, string GETData)
    {

        return await GetHttpWebRequest(url, GETData, "GET");
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
}
