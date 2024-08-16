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

public class ExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;


    private static readonly int SystemExceptionCategory = 99; //Liable to change based on requirements

    private readonly ICallFunction _callFunction;

    public ExceptionHandler(ILogger<ExceptionHandler> logger, ICallFunction callFunction)
    {

        _logger = logger;
        _callFunction = callFunction;
    }

    public async Task CreateSystemExceptionLog(Exception exception, Participant participant, string fileName)
    {
        var url = GetUrlFromEnvironment();
        if (participant.NhsNumber != null)
        {
            participant.ExceptionFlag = "Y";
        }

        var validationException = CreateValidationException(participant.NhsNumber ?? "0", exception, fileName);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    public async Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName)
    {
        var url = GetUrlFromEnvironment();
        var validationException = CreateValidationException(participant.NhsNumber ?? "0", exception, fileName);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    public async Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string NhsNumber, string fileName)
    {
        var url = GetUrlFromEnvironment();
        var validationException = CreateValidationException(NhsNumber ?? "0", exception, "");

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    public async Task<bool> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord)
    {
        var url = GetUrlFromEnvironment();
        participantCsvRecord.Participant.ExceptionFlag = "Y";

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
                ErrorRecord = ruleDetails[1],
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                ExceptionDate = DateTime.UtcNow,
                Category = 1,
                ScreeningName = participantCsvRecord.Participant.ScreeningName,
                ScreeningService = int.Parse(participantCsvRecord.Participant.ScreeningId),
                Cohort = "",
                Fatal = 0
            };

            var exceptionJson = JsonSerializer.Serialize(exception);
            var response = await _callFunction.SendPost(url, exceptionJson);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("there was an error while logging an exception to the database");
                return false;
            }
        }

        return true;
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

    private ValidationException CreateValidationException(string nhsNumber, Exception exception, string fileName)
    {
        // mapping liable to change.
        return new ValidationException
        {
            NhsNumber = nhsNumber,
            DateCreated = DateTime.Now,
            FileName = fileName,
            DateResolved = DateTime.MaxValue,
            RuleId = exception.HResult,
            RuleDescription = exception.Message,
            RuleContent = "System Exception",
            Category = SystemExceptionCategory,
            ScreeningService = 1,
            ExceptionDate = DateTime.UtcNow,
            ErrorRecord = exception.Message,
            ScreeningName = "BSS",
            Cohort = "",
            Fatal = 1
        };

    }

}
