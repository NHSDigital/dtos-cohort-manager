
namespace Common.Interfaces;

using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class SendExceptionToServiceBus : IExceptionSender
{
    private readonly IQueueClient _serviceBusHandler;

    private readonly ILogger<IExceptionSender> _logger;
    private readonly serviceBusValidationConfig _serviceBusValidationConfig;


    public SendExceptionToServiceBus(IQueueClient serviceBusHandler, ILogger<IExceptionSender> logger, IOptions<serviceBusValidationConfig> httpValidationConfig)
    {
        _serviceBusHandler = serviceBusHandler;
        _logger = logger;
    }
    public async Task<bool> sendToCreateException(ValidationException validationException)
    {
        var serviceBusTopicName = _serviceBusValidationConfig.serviceBusTopicName;
        if (string.IsNullOrWhiteSpace(serviceBusTopicName))
        {
            _logger.LogError("The service bus topic was not set and therefore we cannot sent exception to topic");
            return false;
        }
        return await _serviceBusHandler.AddAsync<ValidationException>(validationException, serviceBusTopicName);
    }
}