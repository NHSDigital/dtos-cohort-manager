namespace Common;

using System.Net;
using Model;
using Model.Enums;
using RulesEngine.Models;

/// <summary>
/// Various methods for creating an exception and writing to the exception management table.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Creates a system exception.
    /// </summary>
    /// <param name="exception">The exception to be written to the database.</param>
    /// <param name="participant">The participant that created the exception.</param>
    /// <param name="fileName">The file name of the file containing the participant.</param>
    Task CreateSystemExceptionLog(Exception exception, IParticipant participant, ExceptionCategory category = ExceptionCategory.Non);
    Task CreateSystemExceptionLog(string exceptionMessage, IParticipant participant, ExceptionCategory category = ExceptionCategory.Non);
    /// <summary>
    /// Overload for creating a system exception for errors when handling requests
    /// </summary>
    /// <param name="exception">The exception to be written to the database.</param>
    /// <param name="requestBody">The request body of the request that caused the error</param>
    /// <param name="category">The exception category</param>
    /// <returns></returns>
    Task CreateSystemExceptionLog(Exception exception, string requestBody, ExceptionCategory category = ExceptionCategory.Non);
    /// <summary>
    /// Overloaded method to create a system exception given ServiceNowParticipant.
    /// </summary>
    /// <param name="exception">The exception to be written to the database.</param>
    /// <param name="participant">The participant that created the exception.</param>
    Task CreateSystemExceptionLog(Exception exception, ServiceNowParticipant participant);
    /// <summary>
    /// Creates one or more validation exceptions.
    /// </summary>
    /// <param name="validationErrors">A list of the validation errors</param>
    /// <param name="participantCsvRecord">The participant that triggered them</param>
    Task<bool> CreateValidationExceptionLog(IEnumerable<ValidationRuleResult> validationErrors, Participant participantCsvRecord);
    Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord);
    /// <summary>
    /// Method to create a transformation exception and send it to the DB.
    /// </summary>
    /// <param name="tansformationErrors">RuleResultTree containing the transformations that have failed.</param>
    /// <param name="participant">The participant that caused the exception.</param>
    Task CreateTransformationExceptionLog(IEnumerable<RuleResultTree> transformationErrors, CohortDistributionParticipant participant);
    Task CreateTransformExecutedExceptions(CohortDistributionParticipant participant, string ruleName, int ruleId, ExceptionCategory? exceptionCategory = null);
}
