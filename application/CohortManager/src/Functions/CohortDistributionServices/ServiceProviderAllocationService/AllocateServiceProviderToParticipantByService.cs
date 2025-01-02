namespace NHS.CohortManager.ServiceProviderAllocationService;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class AllocateServiceProviderToParticipantByService
{
    private readonly ILogger _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;

    public AllocateServiceProviderToParticipantByService(ILogger<AllocateServiceProviderToParticipantByService> logger, ICreateResponse createResponse, IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
    }

    [Function("AllocateServiceProviderToParticipantByService")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        // Currently using AllocationConfigRequest Object to deserialize and validate the incoming JSON data
        string requestBody = "";
        AllocationConfigRequestBody configRequest = new AllocationConfigRequestBody();
        _logger.LogInformation("AllocateServiceProviderToParticipantByService is called...");

        try
        {
            string logMessage;

            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            configRequest = JsonSerializer.Deserialize<AllocationConfigRequestBody>(requestBody);

            // check request parameters
            if (string.IsNullOrEmpty(configRequest.NhsNumber) || string.IsNullOrEmpty(configRequest.Postcode) || string.IsNullOrEmpty(configRequest.ScreeningAcronym))
            {
                logMessage = $"One or more of the required parameters is missing. NhsNumber: REDACTED Postcode: {configRequest.Postcode} ScreeningService: {configRequest.ScreeningAcronym}";
                _logger.LogError(logMessage);

                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new Exception(logMessage), configRequest.NhsNumber, "", "", configRequest.ErrorRecord);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, logMessage);
            }

            // check config file
            string configFilePath = Path.Combine(Environment.CurrentDirectory, "ConfigFiles", "allocationConfig.json");
            if (!File.Exists(configFilePath))
            {
                logMessage = $"Cannot find allocation configuration file. Path may be invalid";
                _logger.LogError(logMessage);

                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new Exception(logMessage), configRequest.NhsNumber, "", "", configRequest.ErrorRecord);
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, logMessage);
            }

            string configFile = await File.ReadAllTextAsync(configFilePath);
            var allocationConfigEntries = JsonSerializer.Deserialize<AllocationConfigDataList>(configFile);

            // find the best match postcode and return the provider
            string serviceProvider = FindBestMatchProvider(allocationConfigEntries.ConfigDataList, configRequest.Postcode, configRequest.ScreeningAcronym);

            // check screening provider
            if (serviceProvider != null)
            {
                _logger.LogInformation("Successfully retrieved the Service Provider");
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, serviceProvider);
            }

            logMessage = $"No matching entry found.";
            _logger.LogError(logMessage);

            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(new Exception(logMessage), configRequest.NhsNumber, "", "", configRequest.ErrorRecord);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, logMessage);

        }

        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, configRequest.NhsNumber, "", "", configRequest.ErrorRecord);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private static string? FindBestMatchProvider(AllocationConfigData[] allocationConfigData, string postCode, string screeningService)
    {

        var result = allocationConfigData
        .Where(item => postCode.StartsWith(item.Postcode, StringComparison.OrdinalIgnoreCase) &&
                item.ScreeningService.Equals(screeningService, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(item => item.Postcode.Length)
        .Select(item => item.ServiceProvider)
        .FirstOrDefault();

        if (result == null)
        {
            return "BS SELECT";
        }
        return result;

    }

}

