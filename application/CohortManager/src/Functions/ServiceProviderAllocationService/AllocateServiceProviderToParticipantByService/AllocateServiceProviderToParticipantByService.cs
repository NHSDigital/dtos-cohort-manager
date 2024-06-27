namespace NHS.CohortManager.ServiceProviderAllocationService;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using RulesEngine.Models;

public class AllocateServiceProviderToParticipantByService
{
    private readonly ILogger _logger;
    private readonly ICreateResponse _createResponse;
    private readonly ICallFunction _callFunction;

    public AllocateServiceProviderToParticipantByService(ILogger<AllocateServiceProviderToParticipantByService> logger, ICreateResponse createResponse, ICallFunction callFunction)
    {
        _logger = logger;
        _createResponse = createResponse;
        _callFunction = callFunction;
    }

    [Function("AllocateServiceProviderToParticipantByService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {

        // Cohort Distribution still WIP, and nothing is calling this function yet so we use the following as temp values:
        // Post Code: Participant data
        // Screening Service: Hardcoded "BSS" for BS Select
        string screeningService = "BSS";
        string requestBody = "";
        Participant? allocationData = null;

        _logger.LogInformation("AllocateServiceProviderToParticipantByService is called...");

        try
        {
            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            allocationData = JsonSerializer.Deserialize<Participant>(requestBody);

            // use the Rules Engine
            string staticRulesFile = File.ReadAllText("allocationRules.json");
            var staticRules = JsonSerializer.Deserialize<Workflow[]>(staticRulesFile);

            var reSettings = new ReSettings
            {
                CustomTypes = [typeof(Regex)]
            };

            var rulesEngine = new RulesEngine.RulesEngine(staticRules, reSettings);

            var ruleParameters = new[] {
                new RuleParameter("participant", allocationData),
            };

            var resultList = await rulesEngine.ExecuteAllRulesAsync("AllocationDataValidation", ruleParameters);

            var validationErrors = new List<string>();

            // If the Allocation Data has missing required information for allocation, and call Create Validation Exception
            // if(allocationData == null || string.IsNullOrEmpty(allocationData.Postcode) || string.IsNullOrEmpty(screeningService))
            // {
            //     string requestBodyJson = new ValidationException
            //     {
            //         FileName = null,
            //         NhsNumber = allocationData.NhsNumber,
            //         DateCreated = DateTime.UtcNow,
            //         RuleDescription = ruleDetails[1],
            //         RuleContent = ruleDetails[1],
            //         DateResolved = DateTime.MaxValue,
            //         ScreeningService = screeningService,
            //     }
            // }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Successfully Allocated user to BS Select");

        return response;
    }
}

