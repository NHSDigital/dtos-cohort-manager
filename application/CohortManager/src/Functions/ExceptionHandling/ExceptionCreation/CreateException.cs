namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using Common;
using Data.Database;

public class CreateException
{

    private readonly ILogger<CreateException> _logger;
    private readonly IValidationExceptionData _validationData;
    private readonly ICreateResponse _createResponse;

    public CreateException(
        ILogger<CreateException> logger,
        IValidationExceptionData validationExceptionData)
    {
        _logger = logger;
        _validationData = validationExceptionData;
    }



    [Function("RunCreateException")]
    public async Task Run(
       [ServiceBusTrigger(topicName: "%ExceptionTopic%", subscriptionName: "%ExceptionSubscription%", Connection = "ServiceBusConnectionString", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("completing message", message.Body);
        await messageActions.CompleteMessageAsync(message);
    }
}
