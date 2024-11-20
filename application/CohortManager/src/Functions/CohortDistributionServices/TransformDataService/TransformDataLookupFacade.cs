namespace NHS.CohortManager.CohortDistribution;

using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;

public class TransformDataLookupFacade : ITransformDataLookupFacade
{
    private readonly ILogger<TransformDataLookupFacade> _logger;
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    public TransformDataLookupFacade(ILogger<TransformDataLookupFacade> logger, IDataServiceClient<BsSelectOutCode> outcodeClient)
    {
        _logger = logger;
        _outcodeClient = outcodeClient;
    }

    public bool ValidateOutcode(string postcode)
    {
        var outcode = postcode.Substring(0, postcode.IndexOf(" "));
        _logger.LogInformation("Valdating Outcode: {outcode}",outcode);
        var result = _outcodeClient.GetSingle(outcode).Result;

        return result != null;
    }

    public string GetBsoCode(string postcode){
        var outcode = postcode.Substring(0, postcode.IndexOf(" "));
        _logger.LogInformation("getting BSO Code for  Outcode: {outcode}",outcode);
        var result = _outcodeClient.GetSingle(outcode).Result;

        return result?.BSO;
    }


}
