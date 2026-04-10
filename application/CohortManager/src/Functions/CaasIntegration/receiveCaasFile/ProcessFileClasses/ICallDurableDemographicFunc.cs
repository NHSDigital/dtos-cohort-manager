namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface ICallDurableDemographicFunc
{
    Task<bool> PostDemographicDataAsync(ParticipantDemographic participant, string DemographicFunctionURI, string fileName);

}
