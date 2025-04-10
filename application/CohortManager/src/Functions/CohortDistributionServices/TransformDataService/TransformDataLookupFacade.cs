namespace NHS.CohortManager.CohortDistributionService;

using DataServices.Client;
using Model;
using Common;

public class TransformDataLookupFacade : ITransformDataLookupFacade
{
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    private readonly IDataServiceClient<BsSelectGpPractice> _bsSelectGPPracticeClient;
    public TransformDataLookupFacade(IDataServiceClient<BsSelectOutCode> outcodeClient,
                                    IDataServiceClient<BsSelectGpPractice> bsSelectGPPracticeClient)
    {
        _outcodeClient = outcodeClient;
        _bsSelectGPPracticeClient = bsSelectGPPracticeClient;
    }

    public bool ValidateOutcode(string postcode)
    {
        string outcode = ValidationHelper.ParseOutcode(postcode)
            ?? throw new TransformationException("Postcode format invalid");

        var result = _outcodeClient.GetSingle(outcode).Result;

        return result != null;
    }

    public string GetBsoCode(string postcode){
        string outcode = ValidationHelper.ParseOutcode(postcode)
            ?? throw new TransformationException("Postcode format invalid");

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

        if (gpPractice == null) return string.Empty;

        return gpPractice.BsoCode;
    }
}
