namespace NHS.Screening.ReceiveCaasFile;

using Model;

public interface ICallDurableDemographicFunc
{
    Task<bool> PostDemographicDataAsync(List<ParticipantDemographic> participants, string DemographicFunctionURI, string fileName, List<ParticipantsParquetMap> values);

}
