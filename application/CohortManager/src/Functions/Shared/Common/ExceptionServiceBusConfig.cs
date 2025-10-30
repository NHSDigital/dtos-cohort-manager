namespace Common;

using System.ComponentModel.DataAnnotations;

public class ExceptionServiceBusConfig
{
    [Required]
    public required string ServiceBusConnectionString { get; set; }

    [Required]
    public required string CreateExceptionTopic { get; set; }

    public bool UseServiceBus { get; set; } = false;
}