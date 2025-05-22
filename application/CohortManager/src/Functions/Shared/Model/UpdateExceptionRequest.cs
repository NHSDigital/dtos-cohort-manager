namespace Model;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.ComponentModel.DataAnnotations;

public class UpdateExceptionRequest
{
    [OpenApiProperty(Description = "The ID of the Exception to update")]
    [Required]
    public string ExceptionId { get; set; }

    [OpenApiProperty(Description = "ServiceNow ticket number (optional)")]
    public string? ServiceNowNumber { get; set; }
}

public class ErrorResponse
{
    [OpenApiProperty(Description = "Error message")]
    public string Message { get; set; }

    [OpenApiProperty(Description = "Optional error details")]
    public string? Details { get; set; }

    [OpenApiProperty(Description = "Timestamp of the error")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
