namespace Common;

using System.Net;
using Model;
using RulesEngine.Models;

public interface IExceptionHandler
{
    Task CreateSystemExceptionLog(Exception exception, Participant participant);
    Task CreateSystemExceptionLog(Exception exception, BasicParticipantData participant);
    Task<bool> CreateValidationExceptionLog(IEnumerable<RuleResultTree> validationErrors, ParticipantCsvRecord participantCsvRecord);
}
