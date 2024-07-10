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
using RulesEngine.Models;

public class HandleException : IHandleException
{
    private readonly ILogger<HandleException> _logger;

    private static readonly int SYSTEMEXCEPTIONCATEGORY = 99;

    private readonly ICallFunction _callFunction;

    public HandleException(ILogger<HandleException> logger, ICallFunction callFunction)
    {

        _logger = logger;
        _callFunction = callFunction;
    }

    public async Task<Participant> CreateSystemExceptionLog(Exception exception, Participant participant)
    {
        var url = GetUrlFromEnvironment();
        if (participant.NhsNumber != null)
        {
            participant.ExceptionRaised = "Y";
        }

        var validationException = createValidationException(participant, exception);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
        return participant;
    }

    public async Task<ParticipantCsvRecord> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord)
    {
        var url = GetUrlFromEnvironment();
        participantCsvRecord.Participant.ExceptionRaised = "Y";

        foreach (var error in validationErrors)
        {
            var ruleDetails = error.Rule.RuleName.Split('.');

            var exception = new ValidationException
            {
                RuleId = int.Parse(ruleDetails[0]),
                RuleDescription = ruleDetails[1],
                RuleContent = ruleDetails[1],
                FileName = participantCsvRecord.FileName,
                NhsNumber = participantCsvRecord.Participant.NhsNumber,
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                Category = 1,
                ScreeningService = 1,
                Cohort = "",
                Fatal = 0
            };

            var exceptionJson = JsonSerializer.Serialize(exception);
            var response = await _callFunction.SendPost(url, exceptionJson);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("there was an error while logging an exception to the database");
                break;
            }
        }

        return participantCsvRecord;
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

    private ValidationException createValidationException(Participant participant, Exception exception)
    {
        // mapping liable to change.
        return new ValidationException
        {
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
