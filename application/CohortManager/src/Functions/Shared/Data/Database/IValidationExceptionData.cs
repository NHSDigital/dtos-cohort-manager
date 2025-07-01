namespace Data.Database;

using Model;
using Model.Enums;

public interface IValidationExceptionData
{
    Task<bool> Create(ValidationException exception);
    Task<List<ValidationException>?> GetAllFilteredExceptions(ExceptionStatus? orderByProperty, SortOrder? sortOrder, ExceptionCategory exceptionCategory);
    Task<ValidationException?> GetExceptionById(int exceptionId);
    Task<bool> RemoveOldException(string nhsNumber, string screeningName);
}
