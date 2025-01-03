namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface IValidateDates
{
    public bool ValidateAllDates(Participant participant);
}