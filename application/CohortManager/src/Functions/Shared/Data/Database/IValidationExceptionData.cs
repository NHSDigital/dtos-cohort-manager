namespace Data.Database;

using Model;

public interface IValidationExceptionData
{
    public bool Create(ValidationException exception);
    public List<ValidationException> GetAll();
    void RemoveOldException(string nhsNumber, string screeningName);
}
