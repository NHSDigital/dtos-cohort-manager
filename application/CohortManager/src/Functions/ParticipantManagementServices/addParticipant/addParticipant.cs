/// <summary>
/// Takes a participant from the queue, gets data from the demographic service,
/// validates the participant, then calls create participant, mark as eligible, and create cohort distribution
/// </summary>

namespace addParticipant;

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Common;
using Model;
using System.Threading.Tasks;

public class AddParticipantFunction
{
    private readonly ILogger<AddParticipantFunction> _logger;
    private readonly IAzureQueueStorageHelper _azureQueueStorageHelper;
    private readonly IAddParticipantProcessor _addParticipantProcessor;

    public AddParticipantFunction(ILogger<AddParticipantFunction> logger,  IAzureQueueStorageHelper azureQueueStorageHelper, IAddParticipantProcessor addParticipantProcessor)
    {
        _logger = logger;
        _azureQueueStorageHelper = azureQueueStorageHelper;
        _addParticipantProcessor = addParticipantProcessor;
    }

    [Function("AddParticipantRateControlled")]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo timerInfo,  FunctionContext context)
    {
        _logger.LogInformation("Add Participant Is polling the queue for unread messages");
        var messages = await  _azureQueueStorageHelper.GetItemsFromQueue<BasicParticipantCsvRecord>(32,Environment.GetEnvironmentVariable("AddQueueName"));
        _logger.LogInformation("Add Participant has pulled {Num} Unread Messages to Process",messages.Count);
        foreach(var message in messages)
        {
            await _addParticipantProcessor.AddParticipant(message);
        }
    }

}
