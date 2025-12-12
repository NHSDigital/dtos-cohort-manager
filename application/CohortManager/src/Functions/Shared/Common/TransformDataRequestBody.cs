namespace Common;

using Model;

public class TransformDataRequestBody
{
    public CohortDistributionParticipant Participant { get; set; }
    public CohortDistribution ExistingParticipant { get; set; }
    public string? ServiceProvider { get; set; }
    public string? FileName {get;set;}
}
