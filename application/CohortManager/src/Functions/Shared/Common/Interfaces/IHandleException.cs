namespace Common;

using System.Net;
using Model;

public interface IHandleException
{

    Task<Participant> CreateSystemExceptionLog(Exception exception, Participant participant);
    Task<Participant> CreateValidationExceptionLog(HttpWebResponse response, Participant participant);
}
