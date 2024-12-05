namespace Model;

public class ValidateCohortDistributionRecordBody
{
    public string? NhsNumber { get; set; }
    public string? FileName { get; set; }
    public CohortDistributionParticipant CohortDistributionParticipant { get; set; } = null!;
}
