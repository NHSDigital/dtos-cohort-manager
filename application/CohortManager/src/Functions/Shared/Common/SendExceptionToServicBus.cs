
namespace Common.Interfaces;

using Common;
using Microsoft.Extensions.Logging;
using Model;

public class SendExceptionToServiceBus : IExceptionSender
{
    private readonly IQueueClient _serviceBusHandler;

    private readonly ILogger<IExceptionSender> _logger;

    public SendExceptionToServiceBus(IQueueClient serviceBusHandler, ILogger<IExceptionSender> logger)
    {
        _serviceBusHandler = serviceBusHandler;
        _logger = logger;
    }
    public async Task<bool> sendToCreateException(ValidationException validationException, string serviceBusTopicName)
    {
        if (string.IsNullOrWhiteSpace(serviceBusTopicName))
        {
            _logger.LogError("The service bus topic was not set and therefore we cannot sent exception to topic");
            return false;
        }
        return await _serviceBusHandler.AddAsync<ValidationException>(validationException, serviceBusTopicName);
    }
}