namespace DataServices.Core;
public class DataServiceResponse<TEntity>
{
    public string JsonData {get; set;}
    public string ErrorMessage {get; set;}
}
