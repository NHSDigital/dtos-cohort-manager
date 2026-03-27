namespace Common;

using System.ComponentModel.DataAnnotations;

public class ExceptionServiceBusConfig
{
    [Required]
    public required string ServiceBusConnectionString_client_internal { get; set; }
    [Required]
    public required string CreateExceptionTopic { get; set; }
    public bool ExceptionUseServiceBus { get; set; } = false;
}
