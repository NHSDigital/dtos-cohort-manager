namespace Data.Database;

using Model;
using Model.Enums;

public interface IValidationExceptionData
{
    Task<bool> Create(ValidationException exception);
    Task<List<ValidationException>> GetAllExceptions(bool todayOnly, ExceptionSort? orderByProperty);
    Task<ValidationException> GetExceptionById(int exceptionId);
    Task<bool> RemoveOldException(string nhsNumber, string screeningName);
}
