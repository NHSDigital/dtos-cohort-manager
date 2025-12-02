namespace Data.Database;

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common;
using Model;
using Model.DTO;
using Model.Enums;

public interface IValidationExceptionData
{
    Task<bool> Create(ValidationException exception);
    Task<List<ValidationException>?> GetFilteredExceptions(ExceptionStatus? exceptionStatus, SortOrder? sortOrder, ExceptionCategory exceptionCategory);
    Task<ValidationException?> GetExceptionById(int exceptionId);
    Task<bool> RemoveOldException(string nhsNumber, string screeningName);
    Task<ServiceResponseModel> UpdateExceptionServiceNowId(int exceptionId, string serviceNowId);
    Task<List<ValidationException>?> GetReportExceptions(DateTime? reportDate, ExceptionCategory exceptionCategory);
    Task<IEnumerable<ExceptionManagement>?> GetByFilter(Expression<Func<ExceptionManagement, bool>> filter);
    Task<IQueryable<ValidationException>> GetExceptionsByNhsNumber(string nhsNumber);
    Task<List<ValidationExceptionReport>> GetReportsByNhsNumber(string nhsNumber);
    List<ValidationException> ProcessExceptions(IEnumerable<ExceptionManagement> exceptions);
    List<ValidationExceptionReport> GenerateReports(List<ValidationException> validationExceptions);
}
