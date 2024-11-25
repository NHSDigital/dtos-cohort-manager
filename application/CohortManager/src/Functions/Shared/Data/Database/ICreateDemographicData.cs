namespace Data.Database;

using Model;

public interface ICreateDemographicData
{
    public bool InsertDemographicData(List<Demographic> demographic);
    public Demographic GetDemographicData(string NhsNumber);
}
