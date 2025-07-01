
namespace Common.Interfaces;

using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class SendExceptionToServiceBus : IExceptionSender
{
    private readonly IQueueClient _serviceBusHandler;

    private readonly ILogger<SendExceptionToServiceBus> _logger;
    private readonly ServiceBusValidationConfig _serviceBusValidationConfig;


    public SendExceptionToServiceBus(IQueueClient serviceBusHandler, ILogger<SendExceptionToServiceBus> logger, IOptions<ServiceBusValidationConfig> serviceBusValidationConfig)
    {
        _serviceBusHandler = serviceBusHandler;
        _logger = logger;
        _serviceBusValidationConfig = serviceBusValidationConfig.Value;
    }
    public async Task<bool> sendToCreateException(ValidationException validationException)
    {
        var serviceBusTopicName = _serviceBusValidationConfig.CreateExceptionTopic;
        if (string.IsNullOrWhiteSpace(serviceBusTopicName))
        {
            _logger.LogError("The service bus topic was not set and therefore we cannot sent exception to topic");
            return false;
        }
        return await _serviceBusHandler.AddAsync(validationException, serviceBusTopicName);
    }
}