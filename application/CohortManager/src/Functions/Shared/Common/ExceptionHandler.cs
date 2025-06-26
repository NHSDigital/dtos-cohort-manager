namespace Common;

using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using RulesEngine.Models;

public class ExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;
    private readonly IHttpClientFunction _httpClientFunction;
    private static readonly int DefaultRuleId = 0;
    private readonly string _createExceptionUrl;
    private const string DefaultCohortName = "";
    private const string DefaultScreeningName = "";
    private const string DefaultErrorRecord = "N/A";
    private const string DefaultFileName = "";
    private const string DefaultNhsNumber = "";

    public ExceptionHandler(ILogger<ExceptionHandler> logger, IHttpClientFunction httpClientFunction)
    {

        _logger = logger;
        _httpClientFunction = httpClientFunction;
        _createExceptionUrl = Environment.GetEnvironmentVariable("ExceptionFunctionURL");

        if (_createExceptionUrl == null)
        {
            _logger.LogError("ExceptionFunctionURL environment variable is not set.");
            throw new InvalidOperationException("ExceptionFunctionURL environment variable is not set.");
        }

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

        await _httpClientFunction.SendPost(_createExceptionUrl, JsonSerializer.Serialize(validationException));
    }

    public async Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName)
    {
        var nhsNumber = participant.NhsNumber ?? DefaultNhsNumber;
        var screeningName = participant.ScreeningName ?? DefaultScreeningName;
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, JsonSerializer.Serialize(participant));

        await _httpClientFunction.SendPost(_createExceptionUrl, JsonSerializer.Serialize(validationException));
    }

    public async Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord)
    {
        var validationException = CreateDefaultSystemValidationException(nhsNumber, exception, fileName, screeningName, errorRecord);

        await _httpClientFunction.SendPost(_createExceptionUrl, JsonSerializer.Serialize(validationException));
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

        var response = await _httpClientFunction.SendPost(_createExceptionUrl, JsonSerializer.Serialize(exception));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("There was an error while logging an exception to the database.");
        }


    }

    public async Task CreateSchemaValidationException(BasicParticipantCsvRecord participantCsvRecord, string description)
    {
        var exception = new ValidationException
        {
            RuleId = 0,
            RuleDescription = description,
            FileName = participantCsvRecord.FileName,
            NhsNumber = participantCsvRecord.Participant.NhsNumber,
            ErrorRecord = JsonSerializer.Serialize(participantCsvRecord.Participant),
            DateCreated = DateTime.Now,
            DateResolved = DateTime.MaxValue,
            ExceptionDate = DateTime.Now,
            Category = (int)ExceptionCategory.Schema,
            ScreeningName = participantCsvRecord.Participant.ScreeningName,
            CohortName = DefaultCohortName,
            Fatal = 1

        };

        var response = await _httpClientFunction.SendPost(_createExceptionUrl, JsonSerializer.Serialize(exception));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("There was an error while logging an exception to the database.");
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
                DateCreated = DateTime.Now,
                DateResolved = null,
                ExceptionDate = DateTime.Now,
                Category = (int)ExceptionCategory.File,
                ScreeningName = participant.ScreeningName,
                CohortName = DefaultCohortName,
                Fatal = 0
            };

            var exceptionJson = JsonSerializer.Serialize(exception);
            var response = await _httpClientFunction.SendPost(_createExceptionUrl, exceptionJson);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("There was an error while logging a transformation exception to the database");
            }
        }
    }
    public async Task<ValidationExceptionLog> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord)
    {
        // Handle null or empty collection
        if (validationErrors == null || !validationErrors.Any())
        {
            return new ValidationExceptionLog
            {
                IsFatal = false,
                CreatedException = false
            };
        }

        var errorCount = validationErrors.Count();
        bool isSingleConfusionError = false;

        // Check if we have exactly 1 error and it's Confusion category
        if (errorCount == 1)
        {
            var firstError = validationErrors.First();
            var ruleDetails = firstError.Rule?.RuleName?.Split('.') ?? Array.Empty<string>();

            if (ruleDetails.Length > 2)
            {
                var categoryString = ruleDetails[2];
                if (Enum.TryParse<ExceptionCategory>(categoryString, true, out var categoryEnum))
                {
                    isSingleConfusionError = categoryEnum == ExceptionCategory.Confusion;
                }
            }
        }

        //As per the new changed rule 35, when a record meets this criteria then do not change the exception flag
        // Only set ExceptionFlag if NOT a single Confusion error
        if (!isSingleConfusionError)
        {
            participantCsvRecord.Participant.ExceptionFlag = "Y";
        }


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
                DateCreated = DateTime.Now,
                DateResolved = DateTime.MaxValue,
                ExceptionDate = DateTime.Now,
                Category = GetCategory(Category),
                ScreeningName = participantCsvRecord.Participant.ScreeningName,
                CohortName = DefaultCohortName,
                Fatal = IsFatal
            };

            var exceptionJson = JsonSerializer.Serialize(exception);
            var response = await _httpClientFunction.SendPost(_createExceptionUrl, exceptionJson);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
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
            CreatedException = !isSingleConfusionError  //false for single Confusion error. This is because we want to saved it to cohort distribution table for new change rule 35.
        };
    }

    private static int GetCategory(string category)
    {
        return (int)Enum.Parse(typeof(ExceptionCategory), category, ignoreCase: true);
    }

    public async Task<bool> CreateRecordValidationExceptionLog(string nhsNumber, string fileName, string errorDescription, string screeningName, string errorRecord)
    {
        var validationException = CreateDefaultValidationException(nhsNumber, fileName, errorDescription, screeningName, errorRecord);


        var response = await _httpClientFunction.SendPost(_createExceptionUrl, JsonSerializer.Serialize(validationException));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("There was an error while logging an exception to the database.");
            return false;
        }
        return true;
    }

    public async Task CreateTransformExecutedExceptions(CohortDistributionParticipant participant, string ruleName, int ruleId)
    {
        var exception = new ValidationException
        {
            RuleId = ruleId,
            RuleDescription = $"Participant was transformed as transform rule: {ruleName} was executed",
            FileName = DefaultFileName,
            NhsNumber = participant.NhsNumber,
            ErrorRecord = JsonSerializer.Serialize(participant),
            DateCreated = DateTime.Now,
            DateResolved = DateTime.MaxValue,
            ExceptionDate = DateTime.Now,
            Category = (int)ExceptionCategory.TransformExecuted,
            ScreeningName = participant.ScreeningName,
            CohortName = DefaultCohortName,
            Fatal = 0
        };

        var exceptionJson = JsonSerializer.Serialize(exception);
        var response = await _httpClientFunction.SendPost(_createExceptionUrl, exceptionJson);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError("There was an error while logging a transformation exception to the database");
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
            DateCreated = DateTime.Now,
            FileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName,
            DateResolved = DateTime.MaxValue,
            RuleDescription = exception.Message,
            Category = categoryToSendToDB,
            ScreeningName = string.IsNullOrEmpty(screeningName) ? DefaultScreeningName : screeningName,
            Fatal = 1,
            ErrorRecord = string.IsNullOrEmpty(errorRecord) ? DefaultErrorRecord : errorRecord,
            ExceptionDate = DateTime.Now
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
