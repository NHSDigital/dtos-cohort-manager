namespace Common;

using System.Net;

/// <summary>
/// The methods in this interface use the obsolete WebRequest.Create method.
/// A new shared function called HttpClientFunction.cs has been created to implement these methods using the recommended HttpClient.
/// This interface is still in use until all these methods have been implemented in HttpClientFunction.cs, when it will be replaced by HttpClientFunction.cs.
/// Any new methods that are required should only be implemented in HttpClientFunction.cs.
/// </summary>
public interface ICallFunction
{
    Task<HttpWebResponse> SendPost(string url, string postData);
    Task<string> SendGet(string url);
    Task<string> SendGet(string url, Dictionary<string, string> parameters);
    Task<HttpWebResponse> SendPut(string url, string postData);
    Task<bool> SendDelete(string url);
    Task<string> GetResponseText(HttpWebResponse httpResponseData);
}
