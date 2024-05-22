namespace Common;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;


public class CallFunction : ICallFunction
{
    public async Task<HttpWebResponse> SendPost(string url, string postData)
    {
        var request = (HttpWebRequest)WebRequest.Create(url);

        var data = Encoding.ASCII.GetBytes(postData);

        request.Method = "POST";
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
