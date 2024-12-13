namespace Data.Database;

using Model;

public interface IValidationExceptionData
{
    bool Create(ValidationException exception);
    List<ValidationException> GetAllExceptions(bool todayOnly);
    ValidationException GetExceptionById(int exceptionId);
    bool RemoveOldException(string nhsNumber, string screeningName);
}
