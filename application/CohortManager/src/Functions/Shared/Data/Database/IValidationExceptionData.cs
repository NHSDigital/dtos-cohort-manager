using Model;

namespace Data.Database;

public interface IValidationExceptionData
{
    public bool Create(ValidationException exception);
    public List<ValidationException> GetAll();
    public bool CreateFileValidationException(FileValidationRequestBody exception);
}
