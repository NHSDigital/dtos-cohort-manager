namespace Data.Database;

using System.Data;
using System.Threading.Tasks;
using DataServices.Client;
using Model;
using Model.Enums;


public class CreateDemographicData : ICreateDemographicData
{
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;

    public CreateDemographicData(IDataServiceClient<ParticipantDemographic> participantDemographic)
    {
        _participantDemographic = participantDemographic;
    }

    public async Task<Demographic> GetDemographicData(string nhsNumber)
    {
        long nhsNumberLong;
        if (!long.TryParse(nhsNumber, out nhsNumberLong))
        {
            throw new FormatException("Could not parse NhsNumber");
        }
        var result = await _participantDemographic.GetSingleByFilter(x => x.NhsNumber == nhsNumberLong);
        return result.ToDemographic();
    }
}
