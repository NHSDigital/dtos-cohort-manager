using dtos_cohort_manager_specflow.TestServices;
using TechTalk.SpecFlow;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using dtos_cohort_manager_specflow.Config;
using dtos_cohort_manager_specflow.Contexts;
using dtos_cohort_manager_specflow.Models;
using Microsoft.Extensions.Logging;

namespace dtos_cohort_manager_specflow.Steps.RegressionTests;

[Binding]
public class Epic1_AutomatedRegressionSuiteSteps
{
    private readonly EndToEndFileUploadService _fileUploadService;
    private SmokeTestsContext _smokeTestsContext;


    public Epic1_AutomatedRegressionSuiteSteps(IServiceProvider services, SmokeTestsContext smokeTestsContext, ILogger<Epic1_AutomatedRegressionSuiteSteps> logger)
    {
        _smokeTestsContext = smokeTestsContext;
        _fileUploadService = services.GetRequiredService<EndToEndFileUploadService>();

    }

    [Then(@"verify the NHS numbers in Participant_Management and Participant_Demographic table should match the file data")]
    public async Task ThenVerifyTheNHSNumbersInParticipant_ManagementAndParticipant_DemographicTableShouldMatchTheFileData()
    {

        var recordType = _smokeTestsContext.RecordType.ToString().ToUpper();
        await _fileUploadService.VerifyNhsNumbersAsync(
            "PARTICIPANT_MANAGEMENT",
            _smokeTestsContext.NhsNumbers!,
            recordType,
            "PARTICIPANT_DEMOGRAPHIC"
        );
    }



    [Then(@"verify the NhsNumbers in Participant_Management table should match (.*)")]
    public async Task ThenVerifyTheInParticipant_ManagementShouldMatchAmended(string expectedRecordType)
    {

        await _fileUploadService.VerifyNhsNumbersAsync(
            "PARTICIPANT_MANAGEMENT",
            _smokeTestsContext.NhsNumbers!,
            expectedRecordType.ToUpper());
    }

    [Then(@"the Participant_Demographic table should match the (.*) for the NHS Number")]
    public async Task ThenTheParticipant_DemographicTableShouldMatchTheAmendedAMENDEDNewTestForTheNHSNumber(string expectedGivenName)
    {
        await _fileUploadService.VerifyFieldUpdateAsync("PARTICIPANT_DEMOGRAPHIC", _smokeTestsContext.NhsNumbers.FirstOrDefault(), "GIVEN_NAME", expectedGivenName);
    }

    [Then(@"the NHS Number should have exactly (.*) record in Participant_Management")]
    public async Task ThenTheNHSNumberShouldHaveExactlyRecordInParticipant_Management(int count)
    {
        await _fileUploadService.VerifyNhsNumbersCountAsync("PARTICIPANT_MANAGEMENT", _smokeTestsContext.NhsNumbers.FirstOrDefault(), count);
    }

    [Then(@"the NHS Number should have exactly (.*) record in Participant_Demographic")]
    public async Task ThenTheNHSNumberShouldHaveExactlyRecordInParticipant_Demographic(int count)
    {
        await _fileUploadService.VerifyNhsNumbersCountAsync("PARTICIPANT_DEMOGRAPHIC", _smokeTestsContext.NhsNumbers.FirstOrDefault(), count);
    }


}
