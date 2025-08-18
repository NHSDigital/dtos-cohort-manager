namespace Common;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;
using Common.Interfaces;

public class ExceptionHandler : IExceptionHandler
{
    private readonly IExceptionSender _exceptionSender;
    private readonly ILogger<ExceptionHandler> _logger;
    private const string DefaultCohortName = "";
    private const string DefaultScreeningName = "";
    private const string DefaultErrorRecord = "N/A";
    private const string DefaultFileName = "";
    private const string DefaultNhsNumber = "";

    private const string logErrorMessage = "There was an error while logging an exception to the database.";

    public ExceptionHandler(ILogger<ExceptionHandler> logger, IExceptionSender exceptionSender)
    {
        _logger = logger;
        _exceptionSender = exceptionSender;
    }

    public async Task CreateSystemExceptionLog(Exception exception, IParticipant participant, ExceptionCategory category = ExceptionCategory.Non)
    {
        var validationException = CreateDefaultSystemValidationException(participant, exception.Message, (int)category);

        var isSentSuccessfully = await _exceptionSender.sendToCreateException(validationException);

        if (!isSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
    }

    public async Task CreateSystemExceptionLog(string exceptionMessage, IParticipant participant, ExceptionCategory category = ExceptionCategory.Non)
    {
        var validationException = CreateDefaultSystemValidationException(participant, exceptionMessage, (int)category);

        var isSentSuccessfully = await _exceptionSender.sendToCreateException(validationException);

        if (!isSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
    }

    public async Task CreateSystemExceptionLog(Exception exception, string requestBody, ExceptionCategory category = ExceptionCategory.Non)
    {
        var validationException = new ValidationException()
        {
            RuleId = 0,
            CohortName = DefaultCohortName,
            NhsNumber = DefaultNhsNumber,
            DateCreated = DateTime.UtcNow,
            FileName = DefaultFileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = exception.Message,
            Category = (int) category,
            ScreeningName = DefaultScreeningName,
            Fatal = 1,
            ErrorRecord = requestBody,
            ExceptionDate = DateTime.UtcNow
        };

        var isSentSuccessfully = await _exceptionSender.sendToCreateException(validationException);

        if (!isSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
    }

    public async Task CreateSystemExceptionLog(Exception exception, ServiceNowParticipant participant)
    {
        var validationException = CreateDefaultSystemValidationException(participant.NhsNumber.ToString(), exception, DefaultFileName, DefaultScreeningName, JsonSerializer.Serialize(participant));

        await _exceptionSender.sendToCreateException(validationException);
    }

    public async Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord)
    {
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, errorRecord);

        await _exceptionSender.sendToCreateException(validationException);
    }

    public async Task CreateTransformationExceptionLog(IEnumerable<RuleResultTree> transformationErrors, CohortDistributionParticipant participant)
    {
        foreach (var error in transformationErrors)
        {
            var ruleNumber = int.Parse(error.Rule.RuleName.Split('.')[0]);

            var exception = new ValidationException
            {
                RuleId = ruleNumber,
                RuleDescription = error.ExceptionMessage ?? error.Rule.RuleName,
                FileName = DefaultFileName,
                NhsNumber = participant.NhsNumber,
                ErrorRecord = JsonSerializer.Serialize(participant),
                DateCreated = DateTime.UtcNow,
                DateResolved = null,
                ExceptionDate = DateTime.UtcNow,
                Category = (int)ExceptionCategory.File,
                ScreeningName = participant.ScreeningName,
                CohortName = DefaultCohortName,
                Fatal = 0
            };

            var isSentSuccessfully = await _exceptionSender.sendToCreateException(exception);

            if (!isSentSuccessfully)
            {
                _logger.LogError(logErrorMessage);
            }
        }
    }

    public async Task<bool> CreateValidationExceptionLog(IEnumerable<ValidationRuleResult> validationErrors, Participant participant)
    {
        // Create unable to add to cohort distribution exception
        string message = $"Unable to add to cohort distribution. As participant {participant.ParticipantId} has triggered a validation exception";
        await CreateSystemExceptionLog(new Exception(message), participant);

        foreach (var error in validationErrors)
        {
            var ruleDetails = error.RuleName.Split('.');
            var ruleId = int.Parse(ruleDetails[0]);
            var Category = ruleDetails[2];
            var errorMessage = error.RuleDescription;

            if (!string.IsNullOrEmpty(error.ExceptionMessage))
            {
                errorMessage = error.ExceptionMessage;
                _logger.LogError("an exception was raised while running the rules. Exception Message: {ExceptionMessage}", error.ExceptionMessage);
            }

            var exception = new ValidationException
            {
                RuleId = ruleId,
                RuleDescription = errorMessage ?? ruleDetails[1],
                FileName = participant.Source,
                NhsNumber = participant.NhsNumber,
                ErrorRecord = JsonSerializer.Serialize(participant),
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                ExceptionDate = DateTime.UtcNow,
                Category = GetCategory(Category),
                ScreeningName = participant.ScreeningName,
                CohortName = DefaultCohortName,
            };

            var isSentSuccessfully = await _exceptionSender.sendToCreateException(exception);

            if (!isSentSuccessfully)
            {
                _logger.LogError("There was an error while logging an exception to the database");
                return false;
            }
        }
        return true;
    }

    public async Task CreateTransformExecutedExceptions(CohortDistributionParticipant participant, string ruleName, int ruleId, ExceptionCategory? exceptionCategory = null)
    {

        ExceptionCategory category;
        if (exceptionCategory == null)
        {
            category = ruleId switch
            {
                35 => ExceptionCategory.Confusion,
                60 => ExceptionCategory.Superseded,
                _ => ExceptionCategory.TransformExecuted
            };
        }
        else
        {
            category = exceptionCategory.Value;
        }

        var exception = new ValidationException
        {
            RuleId = ruleId,
            RuleDescription = $"Participant was transformed as transform rule: {ruleName} was executed",
            FileName = DefaultFileName,
            NhsNumber = participant.NhsNumber,
            ErrorRecord = JsonSerializer.Serialize(participant),
            DateCreated = DateTime.UtcNow,
            DateResolved = DateTime.MaxValue,
            ExceptionDate = DateTime.UtcNow,
            Category = (int)category,
            ScreeningName = participant.ScreeningName,
            CohortName = DefaultCohortName,
            Fatal = 0
        };

        bool isSentSuccessfully = await _exceptionSender.sendToCreateException(exception);

        if (!isSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
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
    private static ValidationException CreateDefaultSystemValidationException(string nhsNumber, Exception exception, string fileName, string screeningName, string errorRecord, ExceptionCategory exceptionCategory = ExceptionCategory.Non)
    {
        return new ValidationException()
        {
            RuleId = exception.HResult,
            CohortName = DefaultCohortName,
            NhsNumber = string.IsNullOrEmpty(nhsNumber) ? DefaultNhsNumber : nhsNumber,
            DateCreated = DateTime.UtcNow,
            FileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = exception.Message,
            Category = (int) exceptionCategory,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? DefaultScreeningName : screeningName,
            Fatal = 1,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? DefaultErrorRecord : errorRecord,
            ExceptionDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Method is used to create a default system validation exception for the database
    /// </summary>
    /// <param name="participant">The participant that caused the exception</param>
    /// <param name="exceptionMessage">The error description to send to the DB</param>
    /// <param name="category">The category of the exception</param>
    /// <returns></returns>
    private static ValidationException CreateDefaultSystemValidationException(IParticipant participant, string exceptionMessage, int category)
    {
        return new ValidationException()
        {
            RuleId = 0,
            CohortName = DefaultCohortName,
            NhsNumber = participant.NhsNumber ?? DefaultNhsNumber,
            DateCreated = DateTime.UtcNow,
            FileName = participant.Source ?? DefaultFileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = exceptionMessage,
            Category = category,
            ScreeningName = participant.ScreeningName ?? DefaultScreeningName,
            Fatal = 1,
            ErrorRecord = JsonSerializer.Serialize(participant),
            ExceptionDate = DateTime.UtcNow
        };
    }

    private static int GetCategory(string category)
    {
        return (int)Enum.Parse(typeof(ExceptionCategory), category, ignoreCase: true);
    }
}
