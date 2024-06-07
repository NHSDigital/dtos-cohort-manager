using Model;

namespace Data.Database;

public interface IValidationData
{
    public bool Create(ValidationException dto);
    public List<ValidationException> GetAll();
}
