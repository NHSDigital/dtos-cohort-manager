namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.ServiceBus;

public class CreateException
{
    [Function("RunCreateException")]
    public async Task Run(
       [ServiceBusTrigger(topicName: "%ExceptionTopic%", subscriptionName: "%ExceptionSubscription%", Connection = "ServiceBusConnectionString", AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        await messageActions.CompleteMessageAsync(message);
    }


}
