namespace Data.Database;

using Model;

public interface IValidationExceptionData
{
    bool Create(ValidationException exception);
    List<ValidationException> GetAll();
    bool RemoveOldException(string nhsNumber, string screeningName);
}
