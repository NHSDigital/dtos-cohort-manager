namespace Data.Database;

using Model;

public interface ICreateDemographicData
{
    Task<bool> InsertDemographicData(List<Demographic> demographic);
    Demographic GetDemographicData(string NhsNumber);
}
