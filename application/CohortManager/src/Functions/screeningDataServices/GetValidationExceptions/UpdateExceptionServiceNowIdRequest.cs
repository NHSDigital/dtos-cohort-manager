namespace NHS.CohortManager.ScreeningDataServices;

public class UpdateExceptionServiceNowIdRequest
{
    public int ExceptionId { get; set; }
    public required string ServiceNowId { get; set; }
}
