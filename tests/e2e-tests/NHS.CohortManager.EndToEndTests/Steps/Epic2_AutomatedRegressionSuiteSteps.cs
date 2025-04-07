namespace NHS.CohortManager.EndToEndTests.Steps;

using NHS.CohortManager.EndToEndTests.TestServices;
using Reqnroll;
using Microsoft.Extensions.DependencyInjection;
using NHS.CohortManager.EndToEndTests.Contexts;
using NHS.CohortManager.EndToEndTests.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Linq;



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

    [Then(@"the ""(.*)"" table should contain an ""(.*)"" set to (.*)")]
    public async Task ThenTheTableShouldContainAnSetTo(string tableName, string columnName, int flag)
    {

    await _fileUploadService.VerifyFieldUpdateAsync(tableName, columnName, flag.ToString(), _endtoendTestsContext.NhsNumbers.FirstOrDefault());

    }

}
