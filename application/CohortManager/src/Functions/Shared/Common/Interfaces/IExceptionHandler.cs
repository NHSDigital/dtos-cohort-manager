namespace Common;

using System.Net;
using Model;
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
    Task CreateSystemExceptionLog(Exception exception, Participant participant, string fileName, string category = "");
    /// <summary>
    /// Overloaded method to create a system exception given BasicParticipantData.
    /// </summary>
    /// <param name="exception">The exception to be written to the database.</param>
    /// <param name="participant">The participant that created the exception.</param>
    /// <param name="fileName">The file name of the file containing the participant.</param>
    Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName);
    Task<ValidationExceptionLog> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord);
    Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord);
    Task<bool> CreateRecordValidationExceptionLog(string nhsNumber, string fileName, string errorDescription, string screeningName, string errorRecord);
    Task CreateDeletedRecordException(BasicParticipantCsvRecord participantCsvRecord);
    /// <summary>
    /// Method to create a transformation exception and send it to the DB.
    /// </summary>
    /// <param name="tansformationErrors">RuleResultTree containing the transformations that have failed.</param>
    /// <param name="participant">The participant that caused the exception.</param>
    Task CreateTransformationExceptionLog(IEnumerable<RuleResultTree> transformationErrors, CohortDistributionParticipant participant);
    Task CreateSchemaValidationException(BasicParticipantCsvRecord participantCsvRecord, string description);
    Task CreateTransformExecutedExceptions(CohortDistributionParticipant participant, string ruleName, int ruleId);
    Task CreateExceptionLogsForUnTransformRules(CohortDistributionParticipant participant, string ruleName, int ruleId, int category);
}
