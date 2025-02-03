namespace Data.Database;

using Model;

public interface ICreateDemographicData
{
    Task<Demographic> GetDemographicData(string nhsNumber);
}
