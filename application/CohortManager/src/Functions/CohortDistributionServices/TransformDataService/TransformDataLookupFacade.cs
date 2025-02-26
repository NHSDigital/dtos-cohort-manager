namespace NHS.CohortManager.CohortDistribution;

using DataServices.Client;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
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
        string outcode = ParseOutcode(postcode);

        var result = _outcodeClient.GetSingle(outcode).Result;

        return result != null;
    }

    public string GetBsoCode(string postcode){
        string outcode = ParseOutcode(postcode);

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

    /// <summary>
    /// Gets the outcode (first half of the postcode) from a postcode.
    /// Can handle postcodes in any format (including without spaces)
    /// </summary>
    private static string ParseOutcode(string postcode)
    {
        string pattern = @"^([A-Z]{1,2}[0-9][0-9A-Z]?)\s?[0-9][A-Z]{2}$";

        Match match = Regex.Match(postcode, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        if (!match.Success) throw new TransformationException("Postcode format invalid");

        string outcode = match.Groups[1].Value;

        return outcode;
    }
}
