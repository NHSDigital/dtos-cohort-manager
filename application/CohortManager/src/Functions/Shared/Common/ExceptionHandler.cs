namespace Common;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;
using Common.Interfaces;

/// </summary>
/// Handles the creation and logging of various types of exceptions within the cohort management system.
/// Provides methods to create system exceptions, validation exceptions, transformation exceptions, and other specialized exception types.
/// </summary>

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

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging errors and information.</param>
    /// <param name="exceptionSender">The service responsible for sending exceptions to the database.</param>
    public ExceptionHandler(ILogger<ExceptionHandler> logger, IExceptionSender exceptionSender)
    {
        _logger = logger;
        _exceptionSender = exceptionSender;
    }

    /// <summary>
    /// Creates a system exception log for a participant with full details including NHS number and file information.
    /// Sets the participant's exception flag to "Y" if NHS number is present.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="participant">The participant data associated with the exception.</param>
    /// <param name="fileName">The name of the file being processed when the exception occurred.</param>
    /// <param name="category">Optional category for the exception. If empty, determined automatically.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Creates a system exception log for a basic participant with file context.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="participant">The basic participant data associated with the exception.</param>
    /// <param name="fileName">The name of the file being processed when the exception occurred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName)
    {
        var nhsNumber = participant.NhsNumber ?? DefaultNhsNumber;
        var screeningName = participant.ScreeningName ?? DefaultScreeningName;
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, JsonSerializer.Serialize(participant));

        await _exceptionSender.sendToCreateException(validationException);
    }

    /// <summary>
    /// Creates a system exception log for a ServiceNow participant without file context.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="participant">The ServiceNow participant data associated with the exception.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateSystemExceptionLog(Exception exception, ServiceNowParticipant participant)
    {
        var validationException = CreateDefaultSystemValidationException(participant.NhsNumber.ToString(), exception, DefaultFileName, DefaultScreeningName, JsonSerializer.Serialize(participant));

        await _exceptionSender.sendToCreateException(validationException);
    }

    /// <summary>
    /// Creates a system exception log using only NHS number and related context information.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="nhsNumber">The NHS number of the participant associated with the exception.</param>
    /// <param name="fileName">The name of the file being processed when the exception occurred.</param>
    /// <param name="screeningName">The screening name associated with the participant.</param>
    /// <param name="errorRecord">The serialized error record or participant data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord)
    {
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, errorRecord);

        await _exceptionSender.sendToCreateException(validationException);
    }

    /// <summary>
    /// Creates a deletion record exception for a participant marked for deletion.
    /// </summary>
    /// <param name="participantCsvRecord">The record containing the participant data marked for deletion.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Creates a schema validation exception for a participant record that failed schema validation.
    /// </summary>
    /// <param name="participantCsvRecord">The record that failed schema validation.</param>
    /// <param name="description">The description of the schema validation error.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Creates transformation exception logs for each transformation error encountered during participant processing.
    /// </summary>
    /// <param name="transformationErrors">The collection of transformation rule errors.</param>
    /// <param name="participant">The cohort distribution participant being transformed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Creates validation exception logs for validation errors encountered during participant processing.
    /// Also creates a system exception indicating inability to add to cohort distribution.
    /// </summary>
    /// <param name="validationErrors">The collection of validation rule errors.</param>
    /// <param name="participantCsvRecord">The participant record being validated.</param>
    /// <returns>True if all exceptions were logged successfully; otherwise, false.</returns>
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

    /// <summary>
    /// [Obsolete] Creates validation exception logs using the legacy RulesEngine rule result trees.
    /// </summary>
    /// <param name="validationErrors">The collection of rule result trees from the RulesEngine.</param>
    /// <param name="participantCsvRecord">The participant record being validated.</param>
    /// <returns>A validation exception log containing fatal rule information and creation status.</returns>
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

    /// <summary>
    /// Converts a category string to its corresponding exception category integer value.
    /// </summary>
    /// <param name="category">The category string to parse.</param>
    /// <returns>The integer value of the exception category.</returns>
    private static int GetCategory(string category)
    {
        return (int)Enum.Parse(typeof(ExceptionCategory), category, ignoreCase: true);
    }

    // <summary>
    /// Creates a record validation exception log with custom error description.
    /// </summary>
    /// <param name="nhsNumber">The NHS number of the participant.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <param name="errorDescription">The description of the validation error.</param>
    /// <param name="screeningName">The screening name associated with the participant.</param>
    /// <param name="errorRecord">The serialized error record.</param>
    /// <returns>True if the exception was logged successfully; otherwise, false.</returns>
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

    /// <summary>
    /// Creates transform executed exceptions when transformation rules are successfully applied to a participant.
    /// </summary>
    /// <param name="participant">The cohort distribution participant being transformed.</param>
    /// <param name="ruleName">The name of the transformation rule that was executed.</param>
    /// <param name="ruleId">The ID of the transformation rule.</param>
    /// <param name="exceptionCategory">Optional specific exception category. If null, determined by rule ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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


    /// <summary>
    /// Parses the fatal rule type from a string representation.
    /// </summary>
    /// <param name="fatal">The string representation of the fatal rule type.</param>
    /// <returns>The integer value of the fatal rule type (1 for fatal, 0 for non-fatal).</returns>
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

    /// <summary>
    /// Determines if an NHS number represents a nil return file entry.
    /// </summary>
    /// <param name="nhsNumber">The NHS number to check.</param>
    /// <returns>True if the NHS number is a nil return file indicator; otherwise, false.</returns>
    private static bool IsNilReturnFileNhsNumber(string nhsNumber)
    {
        string[] nilReturnFileNhsNumbers = { "0", "0000000000" };
        return nilReturnFileNhsNumbers.Contains(nhsNumber);
    }

}
