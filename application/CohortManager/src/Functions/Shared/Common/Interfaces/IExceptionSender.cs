

namespace Common.Interfaces;

using Model;

public interface IExceptionSender
{
    /// <summary>
    /// sends a message to a topic or to a service bus topic 
    /// </summary>
    /// <param name="validationException"></param>
    /// <param name="topicOrURL"></param>
    /// <returns></returns>
    Task<bool> sendToCreateException(ValidationException validationException);
}