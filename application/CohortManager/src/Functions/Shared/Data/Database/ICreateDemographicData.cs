using Model;

namespace Data.Database;
public interface ICreateDemographicData
{
    public bool InsertDemographicData(Demographic demographic);
    public Demographic GetDemographicData(string NHSId);
}
