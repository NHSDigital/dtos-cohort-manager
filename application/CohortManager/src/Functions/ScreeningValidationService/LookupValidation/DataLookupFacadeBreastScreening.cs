namespace NHS.CohortManager.ScreeningValidationService;

using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;
using Common;

public class DataLookupFacadeBreastScreening : IDataLookupFacadeBreastScreening
{
    private readonly ILogger<DataLookupFacadeBreastScreening> _logger;
    private readonly IDataServiceClient<BsSelectGpPractice> _gpPracticeServiceClient;
    private readonly IDataServiceClient<BsSelectOutCode> _outcodeClient;
    private readonly IDataServiceClient<CurrentPosting> _currentPostingClient;
    private readonly IDataServiceClient<ExcludedSMULookup> _excludedSMUClient;
    private readonly string[] allPossiblePostingCategories = ["ENGLAND", "IOM", "DMS"];


    public DataLookupFacadeBreastScreening
    (
        ILogger<DataLookupFacadeBreastScreening> logger,
        IDataServiceClient<BsSelectGpPractice> gpPracticeClient,
        IDataServiceClient<BsSelectOutCode> outcodeClient,
        IDataServiceClient<CurrentPosting> currentPostingClient,
        IDataServiceClient<ExcludedSMULookup> excludedSMUClient
    )
    {
        _logger = logger;
        _gpPracticeServiceClient = gpPracticeClient;
        _outcodeClient = outcodeClient;
        _currentPostingClient = currentPostingClient;
        _excludedSMUClient = excludedSMUClient;
    }

    /// <summary>
    /// Used in rule 36 in the lookup rules, and rule 54 in the cohort rules.
    /// Validates the participants primary care provider (GP practice code)
    /// </summary>
    /// <param name="primaryCareProvider">The participant's primary care provider.</param>
    /// <returns>bool, whether or not the GP practice code exists in the DB.<returns>
    public bool CheckIfPrimaryCareProviderExists(string primaryCareProvider)
    {
        _logger.LogInformation("Checking Primary Care Provider {PrimaryCareProvider} Exists", primaryCareProvider);
        var result = _gpPracticeServiceClient.GetSingle(primaryCareProvider).Result;
        return result != null;
    }

    /// <summary>
    /// Used in rule 54 in the cohort rules. Validates the participants outcode (1st part of the postcode)
    /// </summary>
    /// <param name="postcode">The participant's postcode.</param>
    /// <returns>bool, whether or not the outcode code exists in the DB.<returns>
    public bool ValidateOutcode(string postcode)
    {
        var outcode = ValidationHelper.ParseOutcode(postcode);
        _logger.LogInformation("Validating Outcode: {Outcode}", outcode);
        var result = _outcodeClient.GetSingle(outcode!).Result;

        return result != null;
    }

    /// Used in rule 58 of the lookup rules.
    /// Validates that the current posting exists, and that it is in the cohort and in use.
    /// </summary>
    /// <param name="currentPosting">The participant's current posting (area code).</param>
    /// <returns>bool, whether or not the current posting is valid.<returns>
    public bool CheckIfCurrentPostingExists(string? currentPosting)
    {

        var result = _currentPostingClient.GetByFilter(i => i.Posting == currentPosting && i.InUse == "Y").Result;
        if (result == null)
        {
            return false;
        }
        if (result.Any())
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// takes in posting and returns if that posting has a valid posting category in the database
    /// </summary>
    /// <param name="postingCategory"></param>
    /// <returns></returns>
    public bool ValidatePostingCategories(string currentPosting)
    {
        var result = _currentPostingClient.GetSingle(currentPosting).Result;
        if (result == null)
        {
            return false;
        }
        if (allPossiblePostingCategories.Contains(result.PostingCategory))
        {
            return true;
        }
        return false;
    }
    public bool CheckIfPrimaryCareProviderInExcludedSmuList(string primaryCareProvider)
    {
        var result = _excludedSMUClient.GetSingle(primaryCareProvider).Result;
        return result != null;
    }
    public string RetrievePostingCategory(string currentPosting)
    {
        if (string.IsNullOrEmpty(currentPosting))
        {
            return null;
        }
        var result = _currentPostingClient.GetSingle(currentPosting).Result;
        return result.PostingCategory;
    }
}