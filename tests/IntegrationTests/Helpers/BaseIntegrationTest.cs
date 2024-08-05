namespace Tests.Integration.Helpers
{
    public abstract class BaseIntegrationTest
    {
        [TestInitialize]
        public void Initialize()
        {
            SetEnvironmentVariables();
            AdditionalSetup();
        }

        protected void SetEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("AzureWebJobsStorage", TestConfig.Get("AzureWebJobsStorage"));
            Environment.SetEnvironmentVariable("BlobContainerName", TestConfig.Get("BlobContainerName"));
            Environment.SetEnvironmentVariable("DemographicURI", TestConfig.Get("Endpoints:DemographicDataService:Url"));
            Environment.SetEnvironmentVariable("PMSAddParticipant", TestConfig.Get("Endpoints:AddParticipant:Url"));
            Environment.SetEnvironmentVariable("PMSUpdateParticipant", TestConfig.Get("Endpoints:UpdateParticipant:Url"));
            Environment.SetEnvironmentVariable("PMSRemoveParticipant", TestConfig.Get("Endpoints:RemoveParticipant:Url"));
            Environment.SetEnvironmentVariable("StaticValidationURL", TestConfig.Get("Endpoints:StaticValidation:Url"));
            Environment.SetEnvironmentVariable("FileValidationURL", TestConfig.Get("Endpoints:FileValidation:Url"));
            Environment.SetEnvironmentVariable("LookupValidation", TestConfig.Get("Endpoints:LookupValidation:Url"));
            Environment.SetEnvironmentVariable("ProcessParticipant", TestConfig.Get("Endpoints:ProcessParticipant:Url"));
            Environment.SetEnvironmentVariable("MarkParticipantAsEligible", TestConfig.Get("Endpoints:MarkParticipantAsEligible:Url"));
            Environment.SetEnvironmentVariable("MarkParticipantAsIneligible", TestConfig.Get("Endpoints:MarkParticipantAsIneligible:Url"));
            Environment.SetEnvironmentVariable("CreateException", TestConfig.Get("Endpoints:CreateException:Url"));
        }

        protected virtual void AdditionalSetup()
        {
            // Override this in derived classes to add additional setup if needed
        }
    }
}
