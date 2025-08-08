namespace Model.DTO;

public class GpCodeUpdateRequestDto
{
    public required string NhsNumber { get; set; }
    public required string PrimaryCareProvider { get; set; }
    public bool IsAmendParticipant { get; set; }
}
