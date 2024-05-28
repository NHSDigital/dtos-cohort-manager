namespace Data.Database;

public interface IValidationData
{
    public bool UpdateRecords(SQLReturnModel sqlToExecute);
    public List<ValidationDataDto> GetAllBrokenRules();
}
