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
    private static readonly int DefaultRuleId = 0;
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

    public async Task CreateSystemExceptionLog(Exception exception, Participant participant, string fileName, string category = "")
    {
        if (participant.NhsNumber != null)
        {
            participant.ExceptionFlag = "Y";
        }

        var nhsNumber = participant.NhsNumber ?? DefaultNhsNumber;
        var screeningName = participant.ScreeningName ?? DefaultScreeningName;
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, JsonSerializer.Serialize(participant), category);

        var isSentSuccessfully = await _exceptionSender.sendToCreateException(validationException);

        if (!isSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
    }

    public async Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName)
    {
        var nhsNumber = participant.NhsNumber ?? DefaultNhsNumber;
        var screeningName = participant.ScreeningName ?? DefaultScreeningName;
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, JsonSerializer.Serialize(participant));

        await _exceptionSender.sendToCreateException(validationException);
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

    public async Task CreateDeletedRecordException(BasicParticipantCsvRecord participantCsvRecord)
    {
        var exception = new ValidationException
        {
            RuleId = 0,
            RuleDescription = "Record received was flagged for deletion",
            FileName = participantCsvRecord.FileName,
            NhsNumber = participantCsvRecord.BasicParticipantData.NhsNumber,
            ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.BasicParticipantData),
            DateCreated = DateTime.UtcNow,
            DateResolved = DateTime.MaxValue,
            ExceptionDate = DateTime.UtcNow,
            Category = (int)ExceptionCategory.DeleteRecord,
            ScreeningName = participantCsvRecord.BasicParticipantData.ScreeningName,
            CohortName = DefaultCohortName,
            Fatal = 1

        };

        var exceptionSentSuccessfully = await _exceptionSender.sendToCreateException(exception);
        if (!exceptionSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
    }

    public async Task CreateSchemaValidationException(BasicParticipantCsvRecord participantCsvRecord, string description)
    {
        var exception = new ValidationException
        {
            RuleId = 0,
            RuleDescription = description,
            FileName = participantCsvRecord.FileName,
            NhsNumber = participantCsvRecord.BasicParticipantData.NhsNumber,
            ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.BasicParticipantData),
            DateCreated = DateTime.UtcNow,
            DateResolved = DateTime.MaxValue,
            ExceptionDate = DateTime.UtcNow,
            Category = (int)ExceptionCategory.Schema,
            ScreeningName = participantCsvRecord.BasicParticipantData.ScreeningName,
            CohortName = DefaultCohortName,
            Fatal = 1

        };

        var isSentSuccessfully = await _exceptionSender.sendToCreateException(exception);

        if (!isSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
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

    public async Task<bool> CreateValidationExceptionLog(IEnumerable<ValidationRuleResult> validationErrors, ParticipantCsvRecord participantCsvRecord)
    {
        participantCsvRecord.Participant.ExceptionFlag = "Y";

        // Create unable to add to cohort distribution exception
        string message = $"Unable to add to cohort distribution. As participant {participantCsvRecord.Participant.ParticipantId} has triggered a validation exception";
        await CreateSystemExceptionLog(new Exception(message), participantCsvRecord.Participant, participantCsvRecord.FileName);

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
                FileName = participantCsvRecord.FileName,
                NhsNumber = participantCsvRecord.Participant.NhsNumber,
                ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.Participant),
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                ExceptionDate = DateTime.UtcNow,
                Category = GetCategory(Category),
                ScreeningName = participantCsvRecord.Participant.ScreeningName,
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

    [Obsolete("Use the above overload")]
    public async Task<ValidationExceptionLog> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord)
    {
        participantCsvRecord.Participant.ExceptionFlag = "Y";

        var foundFatalRule = false;
        foreach (var error in validationErrors)
        {
            var ruleDetails = error.Rule.RuleName.Split('.');
            var ruleId = int.Parse(ruleDetails[0]);
            var Category = ruleDetails[2];
            var errorMessage = (string)error.ActionResult.Output;

            var IsFatal = ParseFatalRuleType(ruleDetails[3]);
            if (IsFatal == 1)
            {
                foundFatalRule = true;
                _logger.LogInformation("A Fatal rule has been found and the record with NHD ID: {NhsNumber} will not be added to the database.", participantCsvRecord.Participant.ParticipantId);
            }

            if (!string.IsNullOrEmpty(error.ExceptionMessage))
            {
                errorMessage = error.ExceptionMessage;
                _logger.LogError("an exception was raised while running the rules. Exception Message: {exceptionMessage}", error.ExceptionMessage);
            }

            var exception = new ValidationException
            {
                RuleId = ruleId,
                RuleDescription = errorMessage ?? ruleDetails[1],
                FileName = participantCsvRecord.FileName,
                NhsNumber = participantCsvRecord.Participant.NhsNumber,
                ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.Participant),
                DateCreated = DateTime.UtcNow,
                DateResolved = DateTime.MaxValue,
                ExceptionDate = DateTime.UtcNow,
                Category = GetCategory(Category),
                ScreeningName = participantCsvRecord.Participant.ScreeningName,
                CohortName = DefaultCohortName,
                Fatal = IsFatal
            };

            var isSentSuccessfully = await _exceptionSender.sendToCreateException(exception);

            if (!isSentSuccessfully)
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

    private static int GetCategory(string category)
    {
        return (int)Enum.Parse(typeof(ExceptionCategory), category, ignoreCase: true);
    }

    public async Task<bool> CreateRecordValidationExceptionLog(string nhsNumber, string fileName, string errorDescription, string screeningName, string errorRecord)
    {
        var validationException = CreateDefaultValidationException(nhsNumber, fileName, errorDescription, screeningName, errorRecord);


        var isSentSuccessfully = await _exceptionSender.sendToCreateException(validationException);

        if (!isSentSuccessfully)
        {
            _logger.LogError(logErrorMessage);
        }
        return isSentSuccessfully;
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
            DateCreated = DateTime.UtcNow,
            FileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = errorDescription,
            Category = (int)ExceptionCategory.File,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? DefaultScreeningName : screeningName,
            Fatal = 0,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? DefaultErrorRecord : errorRecord,
            ExceptionDate = DateTime.UtcNow
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
    private static ValidationException CreateDefaultSystemValidationException(string nhsNumber, Exception exception, string fileName, string screeningName, string errorRecord, string category = "")
    {
        int categoryToSendToDB = (int)ExceptionCategory.Non;
        if (string.IsNullOrEmpty(category))
        {
            if (IsNilReturnFileNhsNumber(nhsNumber))
            {
                categoryToSendToDB = (int)ExceptionCategory.NilReturnFile;
            }
            else
            {
                categoryToSendToDB = (int)ExceptionCategory.File;
            }
        }
        else
        {
            categoryToSendToDB = GetCategory(category);
        }


        return new ValidationException()
        {
            RuleId = exception.HResult,
            CohortName = DefaultCohortName,
            NhsNumber = string.IsNullOrEmpty(nhsNumber) ? DefaultNhsNumber : nhsNumber,
            DateCreated = DateTime.UtcNow,
            FileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = exception.Message,
            Category = categoryToSendToDB,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? DefaultScreeningName : screeningName,
            Fatal = 1,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? DefaultErrorRecord : errorRecord,
            ExceptionDate = DateTime.UtcNow
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

    private static bool IsNilReturnFileNhsNumber(string nhsNumber)
    {
        string[] nilReturnFileNhsNumbers = { "0", "0000000000" };
        return nilReturnFileNhsNumbers.Contains(nhsNumber);
    }

}
