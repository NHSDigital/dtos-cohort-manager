namespace Data.Database;

using Model;

public interface ICreateDemographicData
{
    public bool InsertDemographicData(Demographic demographic);
    public Demographic GetDemographicData(string NhsNumber);
}
