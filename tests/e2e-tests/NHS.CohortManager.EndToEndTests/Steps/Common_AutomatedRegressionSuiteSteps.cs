namespace NHS.CohortManager.EndToEndTests.Steps;

using NHS.CohortManager.EndToEndTests.TestServices;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using NHS.CohortManager.EndToEndTests.Config;
using NHS.CohortManager.EndToEndTests.Contexts;
using NHS.CohortManager.EndToEndTests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;



[Binding]
public class Common_AutomatedRegressionSuiteSteps
{
    private readonly EndToEndFileUploadService _fileUploadService;

    private readonly AppSettings _appSettings;
    private EndToEndTestsContext _endtoendTestsContext;


    public Common_AutomatedRegressionSuiteSteps(IServiceProvider services, AppSettings appSettings, EndToEndTestsContext endtoendTestsContext, ILogger<Epic1_AutomatedRegressionSuiteSteps> logger)
    {
        _appSettings = appSettings;
        _endtoendTestsContext = endtoendTestsContext;
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

        _endtoendTestsContext.FilePath = filePath;
        _endtoendTestsContext.RecordType = (RecordTypesEnum)Enum.Parse(typeof(RecordTypesEnum), recordType, ignoreCase: true);

        _endtoendTestsContext.NhsNumbers = nhsNumbersData.Split(',', StringSplitOptions.TrimEntries).ToList();
    }

    [Given(@"the file is uploaded to the Blob Storage container")]
    [When(@"the file is uploaded to the Blob Storage container")]
    public async Task WhenFileIsUploaded()
    {
        var filePath = _endtoendTestsContext.FilePath;
        await _fileUploadService.UploadFileAsync(filePath);
    }

    [Given(@"the NHS numbers in the database should match the file data")]
    [Then(@"the NHS numbers in the database should match the file data")]
    public async Task ThenVerifyNhsNumbersInDatabase()
    {
        await _fileUploadService.VerifyNhsNumbersAsync("BS_COHORT_DISTRIBUTION", _endtoendTestsContext.NhsNumbers!);
    }
}
