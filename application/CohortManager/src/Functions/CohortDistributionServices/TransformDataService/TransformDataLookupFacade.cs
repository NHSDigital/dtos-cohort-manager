namespace NHS.CohortManager.CohortDistributionService;

using DataServices.Client;
using Model;
using Common;
using Microsoft.Extensions.Logging;

public class TransformDataLookupFacade : ITransformDataLookupFacade
{
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    private readonly IDataServiceClient<BsSelectGpPractice> _bsSelectGPPracticeClient;
    private readonly IDataServiceClient<LanguageCode> _languageCodeClient;
    private readonly IDataServiceClient<ExcludedSMULookup> _excludedSMUClient;

    private Dictionary<string, string> _excludedSMUData = new();

    private readonly ILogger<ITransformDataLookupFacade> _logger;
    public TransformDataLookupFacade(IDataServiceClient<BsSelectOutCode> outcodeClient,
                                    IDataServiceClient<BsSelectGpPractice> bsSelectGPPracticeClient,
                                    IDataServiceClient<LanguageCode> languageCodeClient,
                                    IDataServiceClient<ExcludedSMULookup> excludedSMUClient,
                                    ILogger<ITransformDataLookupFacade> logger)
    {
        _outcodeClient = outcodeClient;
        _bsSelectGPPracticeClient = bsSelectGPPracticeClient;
        _languageCodeClient = languageCodeClient;
        _excludedSMUClient = excludedSMUClient;
        _logger = logger;
    }

    public async Task InitAsync()
    {
        if (_excludedSMUData.Any())
        {
            _logger.LogInformation("the excludedSMUData is already cached");
            return;
        }
        var result = await _excludedSMUClient.GetAll();
        _excludedSMUData = result
            .Select(x => x.GpPracticeCode)
            .ToDictionary(x => x, x => x);
    }

    public Dictionary<string, string> ExcludedSMUList()
    {
        return _excludedSMUData;
    }

    public bool ValidateOutcode(string postcode)
    {
        string outcode = ValidationHelper.ParseOutcode(postcode)
            ?? throw new TransformationException("Postcode format invalid");

        var result = _outcodeClient.GetSingle(outcode).Result;

        return result != null;
    }

    /// <summary>
    /// Used in rule 00 in the transform rules. Validates the participants preferred language code.
    /// </summary>
    /// <param name="languageCode">The participant's preferred language code.</param>
    /// <returns>bool, whether or not the language code exists in the DB.<returns>
    public bool ValidateLanguageCode(string languageCode)
    {
        var result = _languageCodeClient.GetSingle(languageCode).Result;
        return result != null;
    }

    public string GetBsoCode(string postcode)
    {
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
