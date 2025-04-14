namespace Model;
public class CohortRequestAudit
{
    public Guid RequestId { get; set; }
    public string? StatusCode { get; set; }
    public string? CreatedDateTime { get; set; }
}
