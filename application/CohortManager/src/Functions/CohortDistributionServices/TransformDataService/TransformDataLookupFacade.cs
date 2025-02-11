namespace NHS.CohortManager.CohortDistribution;

using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;

public class TransformDataLookupFacade : ITransformDataLookupFacade
{
    private readonly ILogger<TransformDataLookupFacade> _logger;
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    private readonly IDataServiceClient<BsSelectGpPractice> _bsSelectGPPracticeClient;
    public TransformDataLookupFacade(ILogger<TransformDataLookupFacade> logger,
                                    IDataServiceClient<BsSelectOutCode> outcodeClient,
                                    IDataServiceClient<BsSelectGpPractice> bsSelectGPPracticeClient)
    {
        _logger = logger;
        _outcodeClient = outcodeClient;
        _bsSelectGPPracticeClient = bsSelectGPPracticeClient;
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


    /// <summary>
    /// Used in the 4 chained ParticipantNotRegisteredToGPWithReasonForRemoval rules.
    /// Gets the participant's BSO code using their existing primary care provider.
    /// </summary>
    /// <param name="primaryCareProvider">The participant's existing primary care provider.</param>
    /// <returns>string, the participant's BSO code.<returns>
    public string GetBsoCodeUsingPCP(string primaryCareProvider)
    {
        var gpPractice = _bsSelectGPPracticeClient.GetSingle(primaryCareProvider).Result;

        if (gpPractice == null)
        {
            return string.Empty;
        }
        return gpPractice.BsoCode;
    }
}
