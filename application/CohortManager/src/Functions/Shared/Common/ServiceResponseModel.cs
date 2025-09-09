namespace Common;

using System.Net;

public class ServiceResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public HttpStatusCode StatusCode { get; set; }
}
