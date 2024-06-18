namespace Data.Database;

using Model;

public interface ICreateDemographicData
{
    public bool InsertDemographicData(Participant participant);
    public Demographic GetDemographicData(string NHSId);
}
