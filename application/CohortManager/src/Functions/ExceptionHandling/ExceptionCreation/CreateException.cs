namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.ServiceBus;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using System.Threading.Tasks;

public class CreateException
{
    public CreateException() { }


    [Function(nameof(RunCreateException))]
    public async Task<bool> RunCreateException(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        await context.CallActivityAsync<bool>(nameof(AddException));
        return true;
    }


    [Function(nameof(AddException))]
    public bool AddException([ActivityTrigger] FunctionContext executionContext)
    {
        return true;
    }


    [Function("RunCreateException")]
    public async Task Run(
       [ServiceBusTrigger("queue", Connection = "AzureWebJobsStorage", AutoCompleteMessages = false)]
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext,
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        await client.ScheduleNewOrchestrationInstanceAsync(
               nameof(CreateException), CancellationToken.None);

        await messageActions.CompleteMessageAsync(message);
    }


}
