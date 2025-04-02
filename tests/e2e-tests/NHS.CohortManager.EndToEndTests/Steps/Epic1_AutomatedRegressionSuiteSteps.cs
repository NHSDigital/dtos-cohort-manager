namespace NHS.CohortManager.EndToEndTests.Steps;

using NHS.CohortManager.EndToEndTests.TestServices;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using NHS.CohortManager.EndToEndTests.Config;
using NHS.CohortManager.EndToEndTests.Contexts;
using NHS.CohortManager.EndToEndTests;
using NHS.CohortManager.EndToEndTests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using NHS.CohortManager.EndToEndTests.Helpers;
using NUnit.Framework.Internal;

[Binding]
public class Epic1_AutomatedRegressionSuiteSteps
{
    private readonly EndToEndFileUploadService _fileUploadService;

    private readonly BlobStorageHelper _blobStorageHelper;

    private readonly AppSettings _appSettings;
    private EndToEndTestsContext _endtoendTestsContext;


    public Epic1_AutomatedRegressionSuiteSteps(IServiceProvider services, AppSettings appSettings, EndToEndTestsContext endtoendTestsContext, BlobStorageHelper blobStorageHelper)
    {
        _appSettings = appSettings;
        _endtoendTestsContext = endtoendTestsContext;
        _fileUploadService = services.GetRequiredService<EndToEndFileUploadService>();
        _blobStorageHelper = blobStorageHelper;
    }

    [Then(@"verify Parquet file data matches the data in cohort distribution")]
    public async Task ThenVerifyParquetFileDataMatchesTheDataInCohortDistribution()
    {

        string parquetFilePath = _endtoendTestsContext.FilePath;
        var recordType = _endtoendTestsContext.RecordType.ToString().ToUpper();

        await _fileUploadService.ValidateParquetFileAgainstDatabaseAsync(
            parquetFilePath,
            "BS_COHORT_DISTRIBUTION"
        );

    }

    [Then(@"verify the NHS numbers in Participant_Management and Participant_Demographic table should match the file data")]
    public async Task ThenVerifyTheNHSNumbersInParticipantManagementAndParticipantDemographicTableShouldMatchTheFileData()
    {

        var recordType = _endtoendTestsContext.RecordType.ToString().ToUpper();
        await _fileUploadService.VerifyNhsNumbersAsync(
            "PARTICIPANT_MANAGEMENT",
            _endtoendTestsContext.NhsNumbers!,
            recordType,
            "PARTICIPANT_DEMOGRAPHIC"
        );
    }


    [Then(@"the Participant_Demographic table should match the (.*) for the NHS Number")]
    public async Task ThenTheParticipantDemographicTableShouldMatchTheAmendedAMENDEDNewTestForTheNHSNumber(string expectedGivenName)
    {

        await _fileUploadService.VerifyFieldUpdateAsync("PARTICIPANT_DEMOGRAPHIC", "GIVEN_NAME", expectedGivenName, _endtoendTestsContext.NhsNumbers.FirstOrDefault());
    }

    [Then(@"the NHS Number should have the following records count")]
    public async Task ThenTheNHSNumberShouldHaveFollowingRecordsCounts(Table table)
    {
        foreach (var row in table.Rows)
        {
            string tableName = row["TableName"];
            int expectedCount = int.Parse(row["ExpectedCountInTable"]);

            await _fileUploadService.VerifyNhsNumbersCountAsync(
                tableName,
                _endtoendTestsContext.NhsNumbers.FirstOrDefault(),
                expectedCount);
        }
    }


    [Then(@"the uploaded file should exist in blob storage")]
    public async Task ThenTheFileShouldExistInBlobStorage()
    {
        var fileName = Path.GetFileName(_endtoendTestsContext.FilePath);
        var containerName = _appSettings.BlobContainerName;

        var exists = await _blobStorageHelper.DoesBlobExistAsync(fileName, containerName);
        exists.Should().BeTrue($"File {fileName} should exist in container {containerName}");

    }

    [Then(@"the file content should match the original")]
    public async Task ThenTheFileContentShouldMatchTheOriginal()
    {
        var filePath = _endtoendTestsContext.FilePath;
        var containerName = _appSettings.BlobContainerName;

        await _blobStorageHelper.AssertLocalFileMatchesBlobAsync(filePath, containerName);


    }


    [Then(@"there should be (.*) records for the NHS Number in the database")]
    public async Task ThenThereShouldBeRecordsForThe(int count)
    {
        await _fileUploadService.VerifyNhsNumbersCountAsync("BS_COHORT_DISTRIBUTION", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), count);
    }

    [Then(@"the database should match the amended (.*) for the NHS Number")]
    public async Task ThenTheDatabaseShouldMatchTheAmendedForTheNHSNumber(string expectedGivenName)
    {
        await _fileUploadService.VerifyFieldUpdateAsync("BS_COHORT_DISTRIBUTION", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), "GIVEN_NAME", expectedGivenName);
    }



    [Then(@"the Exception table should have rule ID (.*) with description ""(.*)"" for the NHS Number")]
    public async Task ThenTheExceptionTableShouldHaveRuleDetailsForTheNHSNumber(int ruleId, string ruleDescription)
    {
        await _fileUploadService.VerifyFieldUpdateAsync(
            "EXCEPTION_MANAGEMENT",
            "RULE_ID",
            ruleId.ToString(),
            _endtoendTestsContext.NhsNumbers.FirstOrDefault());

        await _fileUploadService.VerifyFieldUpdateAsync(
            "EXCEPTION_MANAGEMENT",
            "RULE_DESCRIPTION",
            ruleDescription,
            _endtoendTestsContext.NhsNumbers.FirstOrDefault());
    }

    [Then(@"the Exception table should have (.*) ""(.*)"" for the file")]
public async Task ThenTheExceptionTableShouldHaveFieldValueForTheFile(string fieldName, string fieldValue)
{
    string fileName = Path.GetFileName(_endtoendTestsContext.FilePath);

    await _fileUploadService.VerifyFieldUpdateAsync(
        "EXCEPTION_MANAGEMENT",
        fieldName,
        fieldValue,
        fileName: fileName);
}

}
