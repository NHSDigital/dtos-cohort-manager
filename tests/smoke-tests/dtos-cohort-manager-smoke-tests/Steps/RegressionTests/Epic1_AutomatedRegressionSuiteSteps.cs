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

    [Then("verify the NHS numbers in Participant_Management and Participant_Demographic table should match the file data")]
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

}
