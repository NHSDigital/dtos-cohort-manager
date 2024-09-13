namespace Common;

using System.Net;
using Model;
using RulesEngine.Models;

public interface IExceptionHandler
{
    Task CreateSystemExceptionLog(Exception exception, Participant participant, string fileName);
    Task CreateSystemExceptionLog(Exception exception, Participant participant);
    Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant, string fileName);
    Task<ValidationExceptionLog> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord);
    Task CreateSystemExceptionLogFromNhsNumber(Exception exception, string NhsNumber, string fileName);
    Task<bool> CreateRecordValidationExceptionLog(ValidationException validation);
}
