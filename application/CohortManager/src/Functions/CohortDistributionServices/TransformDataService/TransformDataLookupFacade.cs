namespace NHS.CohortManager.CohortDistributionService;

using DataServices.Client;
using Model;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.Caching;
using Microsoft.Identity.Client;

public class TransformDataLookupFacade : ITransformDataLookupFacade
{
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    private readonly IDataServiceClient<BsSelectGpPractice> _bsSelectGPPracticeClient;
    private readonly IDataServiceClient<LanguageCode> _languageCodeClient;
    private readonly IDataServiceClient<ExcludedSMULookup> _excludedSMUClient;
    private readonly IMemoryCache _memoryCache;

    private readonly ILogger<TransformDataLookupFacade> _logger;
    public TransformDataLookupFacade(IDataServiceClient<BsSelectOutCode> outcodeClient,
                                    IDataServiceClient<BsSelectGpPractice> bsSelectGPPracticeClient,
                                    IDataServiceClient<LanguageCode> languageCodeClient,
                                    IDataServiceClient<ExcludedSMULookup> excludedSMUClient,
                                    ILogger<TransformDataLookupFacade> logger,
                                    IMemoryCache memoryCache)
    {
        _outcodeClient = outcodeClient;
        _bsSelectGPPracticeClient = bsSelectGPPracticeClient;
        _languageCodeClient = languageCodeClient;
        _excludedSMUClient = excludedSMUClient;
        _memoryCache = memoryCache;
        _logger = logger;
    }


    public async Task<HashSet<string>> GetCachedExcludedSMUValues()
    {
        HashSet<string> excludedSMUData;


        if (!_memoryCache.TryGetValue("excludedSMUData", out excludedSMUData!))
        {
            var allExcludedSMUValues = await _excludedSMUClient.GetAll();

            _logger.LogInformation("now caching excluded SMU data");
            excludedSMUData = allExcludedSMUValues
                .Select(x => x.GpPracticeCode)
                .ToHashSet();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(24));

            _memoryCache.Set("excludedSMUData", excludedSMUData, cacheEntryOptions);
        }

        return excludedSMUData!;
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
