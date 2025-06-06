namespace Model;

public class UpdateExceptionRequest
{
    public required string ExceptionId { get; set; }
    public string? ServiceNowNumber { get; set; }
    public DateTime? ServiceNowCreatedDate { get; set; }
}
