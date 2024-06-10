namespace Data.Database;

public interface IValidationData
{
    public bool Create(ValidationDataDto dto);
    public List<ValidationDataDto> GetAll();
}
