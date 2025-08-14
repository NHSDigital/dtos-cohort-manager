namespace Common;

using System.Net;

public class ServiceResponseModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}
