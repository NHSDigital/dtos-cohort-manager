namespace Common;

using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Model;

public class HandleException : CallFunction, IHandleException
{
    private readonly ILogger<HandleException> _logger;

    public HandleException(ILogger<HandleException> logger)
    {

        _logger = logger;
    }

    public async Task<Participant> CreateSystemExceptionLog(ValidationException validationException, Participant participant)
    {
        var url = GetUrlFromEnvironment();
        if (participant.NhsNumber != null)
        {
            participant.ExceptionFlag = "Y";
        }

        await SendPost(url, JsonSerializer.Serialize(validationException));
        return participant;
    }

    public async Task<Participant> CreateValidationExceptionLog(HttpWebResponse response, Participant participant)
    {
        var url = GetUrlFromEnvironment();

        var validationExceptionJson = await GetResponseText(response);

        participant.ExceptionFlag = "Y";
        await SendPost(url, validationExceptionJson);
        return participant;
    }

    private string GetUrlFromEnvironment()
    {
        var url = Environment.GetEnvironmentVariable("ExceptionFunctionURL");
        if (url == null)
        {
            _logger.LogError("ExceptionFunctionURL environment variable is not set.");
            throw new InvalidOperationException("ExceptionFunctionURL environment variable is not set.");
        }
        return url;
    }
}
