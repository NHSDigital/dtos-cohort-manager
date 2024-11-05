namespace Common;

using System.Net;

public interface ICallFunction
{
    Task<HttpWebResponse> SendPost(string url, string postData);
    Task<string> SendGet(string url);
    Task<string> SendGet(string url, Dictionary<string, string> parameters);
    Task<bool> SendDelete(string uri);
    Task<string> GetResponseText(HttpWebResponse httpResponseData);
}
