namespace Common;

using System.Net;

public interface ICallFunction
{
    Task<HttpWebResponse> SendPost(string url, string postData);
    Task<string> SendGet(string url);

    Task<string> GetResponseText(HttpWebResponse httpResponseData);
}
