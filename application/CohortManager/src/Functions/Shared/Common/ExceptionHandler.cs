namespace Common;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Model;
using RulesEngine.Models;

/// <summary>
/// Various methods for creating an exception and writing to the exception management table.
/// </summary>

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

    /// <summary>
    /// Creates a system exception.
    /// </summary>
    /// <param name="exception">The exception to be written to the database.</param>
    /// <param name="participant">The participant that created the exception.</param>
    /// <param name="fileName">The file name of the file containing the participant.</param>
    public async Task CreateSystemExceptionLog(Exception exception, Participant participant, string fileName)
    {
        var url = GetUrlFromEnvironment();
        if (participant.NhsNumber != null)
        {
            participant.ExceptionFlag = "Y";
        }

        var validationException = CreateValidationException(participant.NhsNumber ?? "0", exception, fileName, participant.ScreeningName);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    /// <summary>
    /// Overloaded method to create a system exception, for use where file name is not accessible.
    /// </summary>
    /// <param name="exception">The exception to be written to the database.</param>
    /// <param name="participant">The participant that created the exception.</param>
    public async Task CreateSystemExceptionLog(Exception exception, Participant participant)
    {
        var url = GetUrlFromEnvironment();
        if (participant.NhsNumber != null)
        {
            participant.ExceptionFlag = "Y";
        }

        var validationException = CreateValidationException(participant.NhsNumber ?? "0", exception, "", participant.ScreeningName);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    /// <summary>
    /// Overloaded method to create a system exception given BasicParticipantData.
    /// </summary>
    /// <param name="exception">The exception to be written to the database.</param>
    /// <param name="participant">The participant that created the exception.</param>
    /// <param name="fileName">The file name of the file containing the participant.</param>
    public async Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName)
    {
        var url = GetUrlFromEnvironment();
        var validationException = CreateValidationException(participant.NhsNumber ?? "0", exception, fileName, participant.ScreeningName);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    public async Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord)
    {
        var url = GetUrlFromEnvironment();
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, errorRecord);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    public async Task<ValidationExceptionLog> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord)
    {
        var url = GetUrlFromEnvironment();
        participantCsvRecord.Participant.ExceptionFlag = "Y";

        var foundFatalRule = false;
        foreach (var error in validationErrors)
        {
            var ruleDetails = error.Rule.RuleName.Split('.');
            var errorMessage = (string)error.ActionResult.Output;

            var IsFatal = ParseFatalRuleType(ruleDetails[2]);
            if (IsFatal == 1)
            {
                foundFatalRule = true;
                _logger.LogInformation("A Fatal rule has been found and the record with NHD ID: {nhsNumber} will not be added to the database.", participantCsvRecord.Participant.ParticipantId);
            }

            var exception = new ValidationException
            {
                RuleId = int.Parse(ruleDetails[0]),
                RuleDescription = errorMessage ?? ruleDetails[1],
                FileName = participantCsvRecord.FileName,
                NhsNumber = participantCsvRecord.Participant.NhsNumber,
                ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.Participant),
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                ExceptionDate = DateTime.UtcNow,
                Category = 1,
                ScreeningName = participantCsvRecord.Participant.ScreeningName,
                Cohort = "",
                Fatal = IsFatal
            };

            var exceptionJson = JsonSerializer.Serialize(exception);
            var response = await _callFunction.SendPost(url, exceptionJson);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("There was an error while logging an exception to the database");
                return new ValidationExceptionLog
                {
                    IsFatal = foundFatalRule,
                    CreatedException = false
                };
            }
        }

        return new ValidationExceptionLog()
        {
            IsFatal = foundFatalRule,
            CreatedException = true
        };
    }

    /// <summary>
    /// Method is used to create a default validation exception for the database
    /// note: errorDescription is the Rule description
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="fileName"></param>
    /// <param name="errorDescription"></param>
    /// <returns></returns>
    private ValidationException CreateDefaultValidationException(string nhsNumber, string fileName, string errorDescription, string screeningName, string errorRecord)
    {

        return new ValidationException()
        {
            RuleId = 1,
            Cohort = "N/A",
            NhsNumber = string.IsNullOrEmpty(nhsNumber) ? "" : nhsNumber,
            DateCreated = DateTime.Now,
            FileName = string.IsNullOrEmpty(fileName) ? "" : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = errorDescription,
            Category = 1,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? "N/A" : screeningName,
            Fatal = 0,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? "N/A" : errorRecord,
            ExceptionDate = DateTime.Now
        };
    }

    /// <summary>
    /// Method is used to create a default system validation exception for the database
    /// note: RuleId is exception status code
    /// note: RuleDescription is exception message
    /// </summary>
    /// <param name="nhsNumber"></param>
    /// <param name="exception"></param>
    /// <param name="fileName"></param>
    /// <param name="screeningName"></param>
    /// <param name="errorRecord"></param>
    /// <returns></returns>
    private ValidationException CreateDefaultSystemValidationException(string nhsNumber, Exception exception, string fileName, string screeningName, string errorRecord)
    {
        return new ValidationException()
        {
            RuleId = 1,
            Cohort = "N/A",
            NhsNumber = string.IsNullOrEmpty(nhsNumber) ? "" : nhsNumber,
            DateCreated = DateTime.Now,
            FileName = string.IsNullOrEmpty(fileName) ? "" : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = exception.Message,
            Category = SystemExceptionCategory,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? "Breast Screening" : screeningName,
            Fatal = 1,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? "N/A" : errorRecord,
            ExceptionDate = DateTime.Now
        };
    }

    public async Task<bool> CreateRecordValidationExceptionLog(string nhsNumber, string fileName, string errorDescription, string screeningName, string errorRecord)
    {
        var validationException = CreateDefaultValidationException(nhsNumber, fileName, errorDescription, screeningName, errorRecord);

        var url = GetUrlFromEnvironment();
        var response = await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("There was an error while logging an exception to the database.");
            return false;
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

    private ValidationException CreateValidationException(string nhsNumber, Exception exception, string fileName, string screeningName)
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
            Category = SystemExceptionCategory,
            ExceptionDate = DateTime.UtcNow,
            ErrorRecord = exception.Message,
            ScreeningName = screeningName ?? "Breast Screening",
            Cohort = "",
            Fatal = 1
        };
    }

    private int ParseFatalRuleType(string fatal)
    {
        var FatalRuleParsed = Enum.TryParse(fatal, out FatalRule IsFatal);
        if (!FatalRuleParsed)
        {
            _logger.LogError("There was a problem parsing the fatal rule Type from the rule details");
            return 0;
        }
        return (int)IsFatal;
    }

}
