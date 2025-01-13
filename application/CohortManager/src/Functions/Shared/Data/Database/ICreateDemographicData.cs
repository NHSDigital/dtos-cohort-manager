namespace Data.Database;

using Model;

public interface ICreateDemographicData
{
    Demographic GetDemographicData(string nhsNumber);
}
