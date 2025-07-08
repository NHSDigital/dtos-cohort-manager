namespace Common;

using System.ComponentModel.DataAnnotations;

public class ExceptionServiceBusConfig
{
    [Required]
    public string ServiceBusConnectionString { get; set; }

    [Required]
    public string CreateExceptionTopic { get; set; }

    public bool UseServiceBus { get; set; } = false;
}