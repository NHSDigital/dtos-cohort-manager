using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace QueueDrainerFunction
{
    public class DrainServiceBusQueue
    {
        private readonly ILogger _logger;

        public DrainServiceBusQueue(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DrainServiceBusQueue>();
        }

        [Function("DrainServiceBusQueue")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
