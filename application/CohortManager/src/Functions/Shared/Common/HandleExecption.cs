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

    private static readonly int SYSTEMEXCEPTIONCATEGORY = 99;

    public HandleException(ILogger<HandleException> logger)
    {

        _logger = logger;
    }

    public async Task<Participant> CreateSystemExceptionLog(Exception exception, Participant participant)
    {
        var url = GetUrlFromEnvironment();
        if (participant.NhsNumber != null)
        {
            participant.ExceptionRaised = "Y";
        }

        var validationException = createValidationException(participant,exception);

        await SendPost(url, JsonSerializer.Serialize(validationException));
        return participant;
    }

    public async Task<Participant> CreateValidationExceptionLog(HttpWebResponse response, Participant participant)
    {
        var url = GetUrlFromEnvironment();

        var validationExceptionJson = await GetResponseText(response);

        participant.ExceptionRaised = "Y";
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

    private ValidationException createValidationException(Participant participant, Exception exception){
        // mapping liable to change.
            return new ValidationException{
                NhsNumber = participant.NhsNumber,
                DateCreated = DateTime.Now,
                DateResolved = DateTime.MaxValue,
                RuleId = exception.HResult,
                RuleDescription = exception.Message,
                RuleContent = "System Exception",
                Category = SYSTEMEXCEPTIONCATEGORY,
                ScreeningService = 1,
                Cohort = "Cohort Temp",
                Fatal = 1
            };

    }
}
