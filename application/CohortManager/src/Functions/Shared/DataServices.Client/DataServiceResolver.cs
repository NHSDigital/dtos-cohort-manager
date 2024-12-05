namespace DataServices.Client;

using Microsoft.Extensions.Logging;

public class DataServiceResolver
{
    private readonly Dictionary<Type, string> _dataServiceUrls;
    public DataServiceResolver(Dictionary<Type, string> dataServiceUrls)
    {
        _dataServiceUrls = dataServiceUrls;
    }

    public string? GetDataServiceUrl(Type type)
    {
        if(_dataServiceUrls.TryGetValue(type, out var url)){
            return url;
        }
        else
        {
            return null;
        }

    }
}
