namespace NHS.Screening.DemographicDurableFunction;

using System.ComponentModel.DataAnnotations;

public class DurableAddFunctionConfig
{
    [Required]
    public string QueueName { get; set; }

    [Required]
    public string QueueConnectionString { get; set; }

    [Required]
    public string AddQueueName { get; set; }
}
