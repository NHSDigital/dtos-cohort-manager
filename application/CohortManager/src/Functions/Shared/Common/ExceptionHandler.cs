namespace Common;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;

/// <summary>
/// Various methods for creating an exception and writing to the exception management table.
/// </summary>

public class ExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;
    private readonly ICallFunction _callFunction;
    private static readonly int DefaultRuleId = 0;
    private const string DefaultCohortName = "";
    private const string DefaultScreeningName = "";
    private const string DefaultErrorRecord = "N/A";
    private const string DefaultFileName = "";
    private const string DefaultNhsNumber = "";

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

        var nhsNumber = participant.NhsNumber ?? DefaultNhsNumber;
        var screeningName = participant.ScreeningName ?? DefaultScreeningName;
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, JsonSerializer.Serialize(participant));

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
        var nhsNumber = participant.NhsNumber ?? DefaultNhsNumber;
        var screeningName = participant.ScreeningName ?? DefaultScreeningName;
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, JsonSerializer.Serialize(participant));

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    public async Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord)
    {
        var url = GetUrlFromEnvironment();
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, errorRecord);

        await _callFunction.SendPost(url, JsonSerializer.Serialize(validationException));
    }

    public async Task CreateDeletedRecordException(BasicParticipantCsvRecord participantCsvRecord)
    {
        var exception = new ValidationException
        {
            RuleId = 0,
            RuleDescription = "Record received was flagged for deletion",
            FileName = participantCsvRecord.FileName,
            NhsNumber = participantCsvRecord.Participant.NhsNumber,
            ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.Participant),
            DateCreated = DateTime.Now,
            DateResolved = DateTime.MaxValue,
            ExceptionDate = DateTime.Now,
            Category = (int)ExceptionCategory.DeleteRecord,
            ScreeningName = participantCsvRecord.Participant.ScreeningName,
            CohortName = DefaultCohortName,
            Fatal = 1

        };
        var url = GetUrlFromEnvironment();
        var response = await _callFunction.SendPost(url, JsonSerializer.Serialize(exception));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("There was an error while logging an exception to the database.");
        }


    }

    public async Task<ValidationExceptionLog> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord)
    {
        var url = GetUrlFromEnvironment();
        participantCsvRecord.Participant.ExceptionFlag = "Y";

        var foundFatalRule = false;
        foreach (var error in validationErrors)
        {
            var ruleDetails = error.Rule.RuleName.Split('.');
            var errorMessage = error.ActionResult.Output is Exception ruleError ? ruleError.Message : (string)error.ActionResult.Output;
            var ruleId = int.Parse(ruleDetails[0]);

            var IsFatal = ParseFatalRuleType(ruleDetails[2]);
            if (IsFatal == 1)
            {
                foundFatalRule = true;
                _logger.LogInformation("A Fatal rule has been found and the record with NHD ID: {NhsNumber} will not be added to the database.", participantCsvRecord.Participant.ParticipantId);
            }

            if (!string.IsNullOrEmpty(error.ExceptionMessage))
            {
                _logger.LogError("an exception was raised while running the rules. Exception Message: {exceptionMessage}", error.ExceptionMessage);
            }

            var exception = new ValidationException
            {
                RuleId = ruleId,
                RuleDescription = errorMessage ?? ruleDetails[1],
                FileName = participantCsvRecord.FileName,
                NhsNumber = participantCsvRecord.Participant.NhsNumber,
                ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.Participant),
                DateCreated = DateTime.Now,
                DateResolved = DateTime.MaxValue,
                ExceptionDate = DateTime.Now,
                Category = ruleId == 51 ? (int)ExceptionCategory.ParticipantLocationRemainingOutsideOfCohort : (int)ExceptionCategory.File,
                ScreeningName = participantCsvRecord.Participant.ScreeningName,
                CohortName = DefaultCohortName,
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
            RuleId = DefaultRuleId,
            CohortName = DefaultCohortName,
            NhsNumber = string.IsNullOrEmpty(nhsNumber) ? DefaultNhsNumber : nhsNumber,
            DateCreated = DateTime.Now,
            FileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = errorDescription,
            Category = (int)ExceptionCategory.File,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? DefaultScreeningName : screeningName,
            Fatal = 0,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? DefaultErrorRecord : errorRecord,
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
            RuleId = exception.HResult,
            CohortName = DefaultCohortName,
            NhsNumber = string.IsNullOrEmpty(nhsNumber) ? DefaultNhsNumber : nhsNumber,
            DateCreated = DateTime.Now,
            FileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = exception.Message,
            Category = IsNilReturnFileNhsNumber(nhsNumber) ? (int)ExceptionCategory.NilReturnFile : (int)ExceptionCategory.File,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? DefaultScreeningName : screeningName,
            Fatal = 1,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? DefaultErrorRecord : errorRecord,
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

    private static bool IsNilReturnFileNhsNumber(string nhsNumber)
    {
        string[] nilReturnFileNhsNumbers = { "0", "0000000000" };
        return nilReturnFileNhsNumbers.Contains(nhsNumber);
    }
}
