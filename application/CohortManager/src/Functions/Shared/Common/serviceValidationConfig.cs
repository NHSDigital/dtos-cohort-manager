namespace Common;

using System.ComponentModel.DataAnnotations;

public class ServiceBusValidationConfig
{
    [Required]
    public string serviceBusConnectionString { get; set; }

    [Required]
    public string serviceBusTopicName { get; set; }
}