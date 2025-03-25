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



[Binding]
public class Epic1_AutomatedRegressionSuiteSteps
{
    private readonly EndToEndFileUploadService _fileUploadService;

    private readonly AppSettings _appSettings;
    private EndToEndTestsContext _endtoendTestsContext;


    public Epic1_AutomatedRegressionSuiteSteps(IServiceProvider services, AppSettings appSettings, EndToEndTestsContext endtoendTestsContext, ILogger<Epic1_AutomatedRegressionSuiteSteps> logger)
    {
        _appSettings = appSettings;
        _endtoendTestsContext = endtoendTestsContext;
        _fileUploadService = services.GetRequiredService<EndToEndFileUploadService>();

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

    [Then(@"verify the NhsNumbers in Participant_Management table should match (.*)")]
    public async Task ThenVerifyTheInParticipantManagementShouldMatchAmended(string expectedRecordType)
    {

        await _fileUploadService.VerifyNhsNumbersAsync(
            "PARTICIPANT_MANAGEMENT",
            _endtoendTestsContext.NhsNumbers!,
            expectedRecordType.ToUpper());
    }

    [Then(@"the Participant_Demographic table should match the (.*) for the NHS Number")]
    public async Task ThenTheParticipantDemographicTableShouldMatchTheAmendedAMENDEDNewTestForTheNHSNumber(string expectedGivenName)
    {
        await _fileUploadService.VerifyFieldUpdateAsync("PARTICIPANT_DEMOGRAPHIC", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), "GIVEN_NAME", expectedGivenName);
    }

    [Then(@"the NHS Number should have exactly (.*) record in Participant_Management")]
    public async Task ThenTheNHSNumberShouldHaveExactlyRecordInParticipantManagement(int count)
    {
        await _fileUploadService.VerifyNhsNumbersCountAsync("PARTICIPANT_MANAGEMENT", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), count);
    }

    [Then(@"the NHS Number should have exactly (.*) record in Participant_Demographic")]
    public async Task ThenTheNHSNumberShouldHaveExactlyRecordInParticipant_Demographic(int count)
    {
        await _fileUploadService.VerifyNhsNumbersCountAsync("PARTICIPANT_DEMOGRAPHIC", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), count);
    }

    [Then(@"the NHS Number should have exactly (.*) record in Cohort_Distribution table")]
    public async Task thereshouldntbenoentryofNHSnumberincohortdistributiontable(int count)
    {
        await _fileUploadService.VerifyNhsNumbersCountAsync("BS_COHORT_DISTRIBUTION", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), count);

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

    [Then(@"the Exception table should contain the below details for the NHS Number")]
    public async Task ThenTheExceptionTableShouldContainTheBelowDetails(Table table)
    {
        var fields = table.Rows.Select(row => new FieldsTable
        {
            FieldName = row["FieldName"],
            FieldValue = row["FieldValue"]
        }).ToList();

        foreach (var field in fields)
        {
            await _fileUploadService.VerifyFieldUpdateAsync("EXCEPTION_MANAGEMENT", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), field.FieldName, field.FieldValue);
        }
    }



}
