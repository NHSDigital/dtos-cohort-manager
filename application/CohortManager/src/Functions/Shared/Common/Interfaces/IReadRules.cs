namespace Common.Interfaces;
public interface IReadRules
{
    Task<string> GetRulesFromDirectory(string jsonFileName);
}
