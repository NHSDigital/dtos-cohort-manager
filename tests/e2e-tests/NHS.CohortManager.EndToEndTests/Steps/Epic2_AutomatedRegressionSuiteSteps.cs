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
public class Epic2_AutomatedRegressionSuiteSteps
{
    private readonly EndToEndFileUploadService _fileUploadService;

    private readonly AppSettings _appSettings;
    private EndToEndTestsContext _endtoendTestsContext;


    public Epic2_AutomatedRegressionSuiteSteps(IServiceProvider services, AppSettings appSettings, EndToEndTestsContext endtoendTestsContext, ILogger<Epic1_AutomatedRegressionSuiteSteps> logger)
    {
        _appSettings = appSettings;
        _endtoendTestsContext = endtoendTestsContext;
        _fileUploadService = services.GetRequiredService<EndToEndFileUploadService>();

    }

    [Then(@"the Participant_Management table should contain an eligibility flag set to true")]
    public async Task ThenTheParticipantTableShouldContainTheBelowDetails(Table table)
    {
        var fields = table.Rows.Select(row => new FieldsTable
        {
            FieldName = row["FieldName"],
            FieldValue = row["FieldValue"]
        }).ToList();

        foreach (var field in fields)
        {
            await _fileUploadService.VerifyFieldUpdateAsync("PARTICIPANT_MANAGEMENT", _endtoendTestsContext.NhsNumbers.FirstOrDefault(), field.FieldName, field.FieldValue);
        }
    }



}
