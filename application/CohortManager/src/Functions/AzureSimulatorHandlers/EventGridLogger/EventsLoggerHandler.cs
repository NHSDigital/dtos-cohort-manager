// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EventGridLogger
{
    public class EventsLoggerHandler
    {
        private readonly ILogger<EventsLoggerHandler> _logger;

        public EventsLoggerHandler(ILogger<EventsLoggerHandler> logger)
        {
            _logger = logger;
        }

        [Function(nameof(EventsLoggerHandler))]
        public void Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            _logger.LogInformation("LOG: A new Create Participant Event created");
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);
        }
    }
}
