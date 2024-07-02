namespace Common;

using Model;


public interface ICreateAggregationData
{
    public bool InsertAggregationData(AggregateParticipant aggregateParticipant);
    public List<AggregateParticipant> ExtractAggregateParticipants();
    public bool UpdateAggregateParticipantAsInactive(string NhsNumber);
}
