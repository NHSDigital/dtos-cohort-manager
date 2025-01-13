namespace Common;

using System.Net;
using Model;
using RulesEngine.Models;

public interface IExceptionHandler
{
    Task CreateSystemExceptionLog(Exception exception, Participant participant, string fileName);
    Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName);
    Task<ValidationExceptionLog> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord);
    Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string nhsNumber, string fileName, string screeningName, string errorRecord);
    Task<bool> CreateRecordValidationExceptionLog(string nhsNumber, string fileName, string errorDescription, string screeningName, string errorRecord);
    Task CreateDeletedRecordException(BasicParticipantCsvRecord participantCsvRecord);
    Task CreateTransformationExceptionLog(IEnumerable<RuleResultTree> transformationErrors, CohortDistributionParticipant participant);
    Task CreateSchemaValidationException(BasicParticipantCsvRecord participantCsvRecord, string description);
}
