namespace Common;

using System.Net;

public interface ICallFunction
{
    Task<HttpWebResponse> SendPost(string url, string postData);
    [Obsolete("SendGetWebRequest should be used in place of this")]
    Task<string> SendGet(string url);
    [Obsolete("SendGetWebRequest should be used in place of this")]
    Task<string> SendGet(string url, Dictionary<string, string> parameters);
    Task<HttpWebResponse> SendGetWebRequest(string url);
    Task<HttpWebResponse> SendGetWebRequest(string url, Dictionary<string, string> parameters);

    Task<HttpWebResponse> SendPut(string url, string postData);
    Task<bool> SendDelete(string url);
    Task<string> GetResponseText(HttpWebResponse httpResponseData);
}
