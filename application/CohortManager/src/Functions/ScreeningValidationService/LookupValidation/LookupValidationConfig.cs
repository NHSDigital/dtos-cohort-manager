namespace NHS.CohortManager.ScreeningValidationService;
public class LookupValidationConfig //TODO: Add required keys and data annotations
{
    public string BsSelectGpPracticeUrl {get;set;}
    public string BsSelectOutCodeUrl {get;set;}
    public string CurrentPostingUrl {get;set;}
    public string ExcludedSMULookupUrl {get;set;}
    public string BlobContainerName {get;set;}
    public string AzureWebJobsStorage {get;set;}
}
