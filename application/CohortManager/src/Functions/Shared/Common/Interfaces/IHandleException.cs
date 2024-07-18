namespace Common;

using System.Net;
using Model;
using RulesEngine.Models;

public interface IExceptionHandler
{
    Task<Participant> CreateSystemExceptionLog(Exception exception, Participant participant);
    Task<BasicParticipantData> CreateSystemExceptionLog(Exception exception, BasicParticipantData participant);
    Task<bool> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord);
}
