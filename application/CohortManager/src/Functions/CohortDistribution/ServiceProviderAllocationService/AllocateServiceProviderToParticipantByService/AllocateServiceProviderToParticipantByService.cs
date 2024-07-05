namespace NHS.CohortManager.ServiceProviderAllocationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

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

        // Cohort Distribution still WIP, and nothing is calling this function yet
        // Currently using AllocationConfigRequest Object to deserialize and validate the incoming JSON data
        string requestBody = "";
        _logger.LogInformation("AllocateServiceProviderToParticipantByService is called...");

        try
        {
            string logMessage;

            using (StreamReader reader = new StreamReader (req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            AllocationConfigRequestBody configRequest = JsonSerializer.Deserialize<AllocationConfigRequestBody>(requestBody);

            // check request parameters
            if (string.IsNullOrEmpty(configRequest.NhsNumber) || string.IsNullOrEmpty(configRequest.Postcode) || string.IsNullOrEmpty(configRequest.ScreeningService))
            {
                logMessage = $"One or more of the required parameters is missing. NhsNumber: {configRequest.NhsNumber} Postcode: {configRequest.Postcode} ScreeningService: {configRequest.ScreeningService}";
                _logger.LogError(logMessage);

                await CallCreateValidationException(configRequest.NhsNumber, logMessage);
                return _createResponse.CreateHttpResponse (HttpStatusCode.BadRequest, req, logMessage);
            }

            // check config file
            string configFilePath = Path.Combine(Environment.CurrentDirectory, "ConfigFiles", "allocationConfig.json");
            if (!File.Exists(configFilePath))
            {
                logMessage = $"Cannot find allocation configuration file. Path may be invalid";
                _logger.LogError(logMessage);

                await CallCreateValidationException(configRequest.NhsNumber, logMessage);
                return _createResponse.CreateHttpResponse (HttpStatusCode.BadRequest, req, logMessage);
            }

            string configFile = File.ReadAllText(configFilePath);
            var allocationConfigEntries = JsonSerializer.Deserialize<AllocationConfigDataList>(configFile);

            // find the best match postcode and return the provider
            string serviceProvider = FindBestMatchProvider (allocationConfigEntries.ConfigDataList, configRequest.Postcode, configRequest.ScreeningService);

            // check screening provider
            if (serviceProvider != null)
            {
                _logger.LogInformation("Successfully retrieved the Service Provider");
                return _createResponse.CreateHttpResponse (HttpStatusCode.OK, req, serviceProvider);
            }

            logMessage = $"No matching entry found.";
            _logger.LogError(logMessage);

            await CallCreateValidationException(configRequest.NhsNumber, logMessage);
            return _createResponse.CreateHttpResponse (HttpStatusCode.BadRequest, req, logMessage);

        }

        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            return _createResponse.CreateHttpResponse (HttpStatusCode.InternalServerError, req);
        }
    }


    private async Task CallCreateValidationException (string nhsNumber, string logMessage)
    {
        _logger.LogError (logMessage);

        _logger.LogInformation("Creating a new Validation exception entry...");

        // create an entry on the validation table
        ValidationException exception = new ValidationException
        {
            FileName = null,
            NhsNumber = nhsNumber ?? null,
            DateCreated = DateTime.UtcNow,
            RuleDescription = "Failed to retrieve the Service Provider from Allocation Config",
            RuleContent = logMessage,
            DateResolved = DateTime.MaxValue,
            ScreeningService = 1,
        };

        string exceptionJson = JsonSerializer.Serialize(exception);
        await _callFunction.SendPost(Environment.GetEnvironmentVariable("CreateValidationExceptionURL"), exceptionJson);
    }
    private string? FindBestMatchProvider (AllocationConfigData[] allocationConfigData, string postCode, string screeningService)
    {
        return allocationConfigData
        .Where(item => postCode.StartsWith(item.Postcode, StringComparison.OrdinalIgnoreCase) &&
                item.ScreeningService.Equals(screeningService, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(item => item.Postcode.Length)
        .Select(item => item.ServiceProvider)
        .FirstOrDefault();
    }

}

