namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using Common;
using Data.Database;
using Azure.Messaging.ServiceBus;

public class CreateException
{
    public CreateException() { }

    [Function("CreateException")]
    public async Task ServiceBusMessageActionsFunction(
    [ServiceBusTrigger("queue", Connection = "AzureWebJobsStorage", AutoCompleteMessages = false)]
    ServiceBusReceivedMessage message,
    ServiceBusMessageActions messageActions)
    {
        await messageActions.CompleteMessageAsync(message);
    }
}
