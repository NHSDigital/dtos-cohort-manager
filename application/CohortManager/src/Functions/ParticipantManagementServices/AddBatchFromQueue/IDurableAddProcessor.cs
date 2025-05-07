using Model;

public interface IDurableAddProcessor
{
    Task<ParticipantCsvRecord?> ProcessAddRecord(string jsonFromQueue);
}