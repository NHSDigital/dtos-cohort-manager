namespace Data.Database;

using Model;

public interface IUpdateAggregateData
{
    public bool UpdateAggregateParticipantAsInactive(string NHSID);
}
