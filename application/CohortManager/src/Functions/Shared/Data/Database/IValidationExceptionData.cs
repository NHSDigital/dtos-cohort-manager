namespace Data.Database;

using Model;

public interface IValidationExceptionData
{
    Task<bool> Create(ValidationException exception);
    Task<List<Model.ValidationException>> GetAllExceptions(bool todayOnly);
    Task<ValidationException> GetExceptionById(int exceptionId);
    Task<bool> RemoveOldException(string nhsNumber, string screeningName);
}
