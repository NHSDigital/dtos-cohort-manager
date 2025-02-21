using dtos_cohort_manager_specflow.TestServices;
using TechTalk.SpecFlow;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using dtos_cohort_manager_specflow.Config;
using dtos_cohort_manager_specflow.Contexts;
using dtos_cohort_manager_specflow.Models;

namespace dtos_cohort_manager_specflow.Steps;

[Binding]
public class FileUploadAndCohortDistributionSteps
{

    private readonly EndToEndFileUploadService _fileUploadService;
    private readonly AppSettings _appSettings;
    private SmokeTestsContext _smokeTestsContext;
    public FileUploadAndCohortDistributionSteps(IServiceProvider services, AppSettings appSettings, SmokeTestsContext smokeTestsContext)
    {
        _appSettings = appSettings;
        _smokeTestsContext = smokeTestsContext;
        _fileUploadService = services.GetRequiredService<EndToEndFileUploadService>();
    }

    [Given(@"the database is cleaned of all records for NHS Numbers: (.*)")]
    public async Task GivenDatabaseIsCleaned(string nhsNumbersString)
    {
        var nhsNumbers = nhsNumbersString.Split(',', StringSplitOptions.TrimEntries);

        // _fileUploadService.CleanDatabaseAsync accepts a list of NHS numbers
        await _fileUploadService.CleanDatabaseAsync(nhsNumbers);
    }

    [Given(@"the application is properly configured")]
    public void GivenApplicationIsConfigured()
    {
        _fileUploadService.Should().NotBeNull("EndToEndFileUploadService is not initialized.");
    }

    [Given(@"file (.*) exists in the configured location for ""(.*)"" with NHS numbers : (.*)")]
    public void GivenFileExistsAtConfiguredPath(string fileName, string? recordType, string nhsNumbersData)
    {
        var folderPath = typeof(FilePaths).GetProperty(recordType!)?.GetValue(_appSettings.FilePaths)?.ToString();
            var filePath = Path.Combine(folderPath!, fileName);

            _smokeTestsContext.FilePath = filePath;
           _smokeTestsContext.RecordType = (RecordTypesEnum)Enum.Parse(typeof(RecordTypesEnum), recordType, ignoreCase: true);

           _smokeTestsContext.NhsNumbers = nhsNumbersData.Split(',', StringSplitOptions.TrimEntries).ToList();
    }

    [Given(@"the file is uploaded to the Blob Storage container")]
    [When(@"the file is uploaded to the Blob Storage container")]
    public async Task WhenFileIsUploaded()
    {
        var filePath =_smokeTestsContext.FilePath;
        await _fileUploadService.UploadFileAsync(filePath);
    }

    [Given(@"the NHS numbers in the database should match the file data")]
    [Then(@"the NHS numbers in the database should match the file data")]
    public async Task ThenVerifyNhsNumbersInDatabase()
    {
        await _fileUploadService.VerifyNhsNumbersAsync("BS_COHORT_DISTRIBUTION", _smokeTestsContext.NhsNumbers!);
    }

    [Then("there should be (.*) records for the NHS Number in the database")]
    public async Task ThenThereShouldBeRecordsForThe(int count)
    {
        await _fileUploadService.VerifyNhsNumbersCountAsync("BS_COHORT_DISTRIBUTION", _smokeTestsContext.NhsNumbers.FirstOrDefault(),count);
    }

    [Then("the database should match the amended (.*) for the NHS Number")]
    public async Task ThenTheDatabaseShouldMatchTheAmendedForTheNHSNumber(string expectedGivenName)
    {
        await _fileUploadService.VerifyFieldUpdateAsync("BS_COHORT_DISTRIBUTION", _smokeTestsContext.NhsNumbers.FirstOrDefault(), "GIVEN_NAME", expectedGivenName);
    }

    [Then("the Exception table should contain the below details for the NHS Number")]
    public async Task ThenTheExceptionTableShouldContainTheBelowDetails(Table table)
    {
        var fields = table.Rows.Select(row => new FieldsTable
        {
            FieldName = row["FieldName"],
            FieldValue = row["FieldValue"]
        }).ToList();

        foreach (var field in fields)
        {
            await _fileUploadService.VerifyFieldUpdateAsync("EXCEPTION_MANAGEMENT", _smokeTestsContext.NhsNumbers.FirstOrDefault(), field.FieldName, field.FieldValue);
        }
    }
}
