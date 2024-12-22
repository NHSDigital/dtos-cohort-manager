namespace Common;

using System.Net;

public interface ICallFunction
{
    Task<HttpWebResponse> SendPost(string url, string postData);
    Task<string> SendGet(string url);
    Task<string> SendGet(string url, Dictionary<string, string> parameters);
    Task<HttpWebResponse> SendPut(string url, string postData);
    Task<bool> SendDelete(string url);
    Task<string> GetResponseText(HttpWebResponse httpResponseData);
}
