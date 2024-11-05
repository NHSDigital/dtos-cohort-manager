namespace Common.Interfaces;

using Model;

public interface IProcessCaasFile
{
    Task ProcessRecordAsync(Participant participant, string filename);
}
