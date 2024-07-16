namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistribution;
using Model;

public class CreateCohortDistribution
{
    private readonly ICallFunction _callFunction;
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<CreateCohortDistribution> _logger;

    public CreateCohortDistribution(ICallFunction callFunction, ICreateResponse createResponse, ILogger<CreateCohortDistribution> logger)
    {
        _callFunction = callFunction;
        _createResponse = createResponse;
        _logger = logger;
    }

    [Function("CreateCohortDistribution")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        // hardcoded data
        var screeningService = "BSS";

        try
        {
            var participantData = await GetParticipantData();
            var serviceProvider = await GetServiceProvider(participantData, screeningService);
            var transformedParticipant = await TransformParticipant(participantData, serviceProvider);
            // call add cohort distribution service

            Console.WriteLine(serviceProvider);
            Console.WriteLine(JsonSerializer.Serialize(transformedParticipant));
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

    }

    private async Task<string> GetResponseText(HttpWebResponse httpResponseData)
    {
        using (Stream stream = httpResponseData.GetResponseStream())
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                var responseText = await reader.ReadToEndAsync();
                return responseText;
            }
        }
    }

    private async Task<Participant> GetParticipantData()
    {
        // just returns hardcoded participant at the mo
        var participant = new Participant
        {
            NhsNumber = "1234567890",
            FirstName = "John",
            Surname = "Smith",
            NamePrefix = "AAAAABBBBBCCCCCDDDDDEEEEEFFFFFGGGGGHHHHH",
            Postcode = "NE63"
        };

        try
        {
            _logger.LogInformation("Called get participant data service");
            return participant;
        }
        catch (Exception ex)
        {
            _logger.LogError("Get participant data service function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    private async Task<string> GetServiceProvider(Participant participant, string screeningService)
    {
        var allocationConfigRequestBody = new AllocationConfigRequestBody
        {
            NhsNumber = participant.NhsNumber,
            Postcode = participant.Postcode,
            ScreeningService = screeningService
        };

        try
        {
            var json = JsonSerializer.Serialize(allocationConfigRequestBody);
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("AllocateScreeningProviderURL"), json);
            var responseText = await GetResponseText(response);

            _logger.LogInformation("Called allocate screening provider service");
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError("Allocate screening provider service function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    private async Task<Participant> TransformParticipant(Participant participant, string serviceProvider)
    {
        var transformDataRequestBody = new TransformDataRequestBody(participant, serviceProvider);

        try
        {
            var json = JsonSerializer.Serialize(transformDataRequestBody);
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("TransformDataServiceURL"), json);
            var responseText = await GetResponseText(response);

            _logger.LogInformation("Called transform data service");
            return JsonSerializer.Deserialize<Participant>(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError("Transform data service function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }
}
