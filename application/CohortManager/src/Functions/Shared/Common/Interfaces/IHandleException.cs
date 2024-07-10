namespace Common;

using System.Net;
using Model;
using RulesEngine.Models;

public interface IHandleException
{
    Task<Participant> CreateSystemExceptionLog(Exception exception, Participant participant);
    Task<ParticipantCsvRecord> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord);
}
