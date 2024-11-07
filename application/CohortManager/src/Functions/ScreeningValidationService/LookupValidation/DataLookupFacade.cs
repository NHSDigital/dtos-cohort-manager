using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;
public class DataLookupFacade : IDataLookupFacade
{
    private ILogger<DataLookupFacade> _logger;
    private readonly IDataServiceClient<BsSelectGpPractice> _gpPracticeServiceClient;
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    private readonly IDataServiceClient<LanguageCode> _languageCodeClient;
    private readonly string[] allPossiblePostingCategories = ["ENGLAND", "IOM", "DMS"];


    public DataLookupFacade
    (
        ILogger<DataLookupFacade> logger,
        IDataServiceClient<BsSelectGpPractice> gpPracticeClient,
        IDataServiceClient<BsSelectOutCode> outcodeClient,
        IDataServiceClient<LanguageCode> languageCodeClient
    )
    {
        _logger = logger;
        _gpPracticeServiceClient = gpPracticeClient;
        _languageCodeClient = languageCodeClient;
        _outcodeClient = outcodeClient;
    }

    /// <summary>
    /// Used in rule 36 in the lookup rules, and rule 54 in the cohort rules.
    /// Validates the participants primary care provider (GP practice code)
    /// </summary>
    /// <param name="primaryCareProvider">The participant's primary care provider.</param>
    /// <returns>bool, whether or not the GP practice code exists in the DB.<returns>
    public bool CheckIfPrimaryCareProviderExists(string primaryCareProvider)
    {
        _logger.LogInformation("Checking Primary Care Provider {primaryCareProvider} Exists", primaryCareProvider);
        var result =  _gpPracticeServiceClient.GetSingle(primaryCareProvider).Result;
        return result != null;
    }

    /// <summary>
    /// Used in rule 54 in the cohort rules. Validates the participants outcode (1st part of the postcode)
    /// </summary>
    /// <param name="postcode">The participant's postcode.</param>
    /// <returns>bool, whether or not the outcode code exists in the DB.<returns>
    public bool ValidateOutcode(string postcode)
    {
        var outcode = postcode.Substring(0, postcode.IndexOf(" "));
        _logger.LogInformation("Valdating Outcode: {outcode}",outcode);
        var result = _outcodeClient.GetSingle(outcode);

        return result != null;
    }
    /// <summary>
    /// Used in rule 00 in the lookup rules. Validates the participants preferred language code.
    /// </summary>
    /// <param name="languageCode">The participant's preferred language code.</param>
    /// <returns>bool, whether or not the language code exists in the DB.<returns>
    public bool ValidateLanguageCode(string languageCode)
    {
        _logger.LogInformation("Valdating Language Code: {languageCode}",languageCode);
        var result = _languageCodeClient.GetSingle(languageCode).Result;
        return result != null;
    }

    public bool CheckIfCurrentPostingExists(string currentPosting)
    {
        throw new NotImplementedException();
    }

    public bool ValidatePostingCategories(string currentPosting)
    {
        throw new NotImplementedException();
    }
}
