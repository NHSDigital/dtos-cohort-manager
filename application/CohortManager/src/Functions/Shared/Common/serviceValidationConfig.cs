namespace Common;

using System.ComponentModel.DataAnnotations;

public class ServiceBusValidationConfig
{
    [Required]
    public string ServiceBusConnectionString { get; set; }

    [Required]
    public string ServiceBusTopicName { get; set; }
}