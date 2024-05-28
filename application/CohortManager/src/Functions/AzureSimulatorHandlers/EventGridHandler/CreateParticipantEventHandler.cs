// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EventGridHandler
{
    public class CreateParticipantEventHandler
    {
        private readonly ILogger<CreateParticipantEventHandler> _logger;

        public CreateParticipantEventHandler(ILogger<CreateParticipantEventHandler> logger)
        {
            _logger = logger;
        }

        [Function(nameof(CreateParticipantEventHandler))]
        public void Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            _logger.LogInformation("A new Create Participant Event created");
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
        }
    }
}
