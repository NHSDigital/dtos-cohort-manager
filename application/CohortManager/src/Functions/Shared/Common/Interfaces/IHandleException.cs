namespace Common;

using System.Net;
using Model;

public interface IHandleException
{

    Task<Participant> CreateSystemExceptionLog(ValidationException validationException, Participant participant);
    Task<Participant> CreateValidationExceptionLog(HttpWebResponse response, Participant participant);
}
