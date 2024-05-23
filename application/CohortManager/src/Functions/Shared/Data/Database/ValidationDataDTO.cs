using Grpc.Core;

namespace Data.Database;

public class ValidationDataDto
{
    public string? Rule { get; set; }
    public DateTime? TimeViolated { get; set; }
    public string? ParticipantId { get; set; }

}