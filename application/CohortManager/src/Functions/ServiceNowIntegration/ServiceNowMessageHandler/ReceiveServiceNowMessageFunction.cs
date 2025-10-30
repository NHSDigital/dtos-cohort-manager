namespace NHS.CohortManager.ServiceNowIntegrationService;

using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using NHS.CohortManager.ServiceNowIntegrationService.Models;
using Model;
using Microsoft.Extensions.Options;
using DataServices.Client;

public class ReceiveServiceNowMessageFunction
{
    private readonly ILogger<ReceiveServiceNowMessageFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IQueueClient _queueClient;
    private readonly ServiceNowMessageHandlerConfig _config;
    private readonly IDataServiceClient<ServicenowCase> _serviceNowCaseClient;

    public ReceiveServiceNowMessageFunction(ILogger<ReceiveServiceNowMessageFunction> logger, ICreateResponse createResponse,
        IQueueClient queueClient, IOptions<ServiceNowMessageHandlerConfig> config, IDataServiceClient<ServicenowCase> serviceNowCaseClient)
    {
        _logger = logger;
        _createResponse = createResponse;
        _queueClient = queueClient;
        _config = config.Value;
        _serviceNowCaseClient = serviceNowCaseClient;
    }

    /// <summary>
    /// Azure Function to receive and validate an incoming ServiceNow message. If the message is valid, it will be sent to an Azure Service Bus Topic.
    /// </summary>
    /// <param name="req">The HTTP request containing the incoming message payload.</param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> object representing the HTTP response.
    /// - Returns a 202 status code if the request is successful.
    /// - Returns a 400 status code if the request is a bad request.
    /// </returns>
    [Function("ReceiveServiceNowMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "servicenow/receive")] HttpRequestData req)
    {
        ReceiveServiceNowMessageRequestBody? requestBody;

        try
        {
            requestBody = await JsonSerializer.DeserializeAsync<ReceiveServiceNowMessageRequestBody>(req.Body);

            if (requestBody == null)
            {
                _logger.LogError("Request body deserialised to null");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            var validationContext = new ValidationContext(requestBody.VariableData);
            var validationResult = new List<ValidationResult>();
            bool isVariableDataValid = Validator.TryValidateObject(requestBody.VariableData, validationContext, validationResult, true);

            if (string.IsNullOrWhiteSpace(requestBody.ServiceNowCaseNumber) || !isVariableDataValid)
            {
                _logger.LogError("Request body failed validation");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            var nhsNumber = long.Parse(requestBody.VariableData.NhsNumber);

            var serviceNowCase = new ServicenowCase
            {
                ServicenowId = requestBody.ServiceNowCaseNumber,
                NhsNumber = nhsNumber,
                Status = ServiceNowStatus.New,
                RecordInsertDatetime = DateTime.UtcNow
            };

            var addSuccess = await _serviceNowCaseClient.Add(serviceNowCase);

            if (!addSuccess)
            {
                _logger.LogError("Failed to save the ServiceNow case to the database. CaseNumber: {CaseNumber}", requestBody.ServiceNowCaseNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            var participant = new ServiceNowParticipant
            {
                ScreeningId = 1, // Hardcoding to the Breast Screening Id
                NhsNumber = nhsNumber,
                FirstName = requestBody.VariableData.FirstName,
                FamilyName = requestBody.VariableData.FamilyName,
                DateOfBirth = requestBody.VariableData.DateOfBirth,
                ServiceNowCaseNumber = requestBody.ServiceNowCaseNumber,
                BsoCode = requestBody.VariableData.BsoCode,
                ReasonForAdding = requestBody.VariableData.ReasonForAdding,
                RequiredGpCode = requestBody.VariableData.RequiredGpCode
            };

            var success = await _queueClient.AddAsync(participant, _config.ServiceNowParticipantManagementTopic);

            if (!success)
            {
                _logger.LogError("Failed to send Participant from ServiceNow to Service Bus Topic. CaseNumber: {CaseNumber}", requestBody.ServiceNowCaseNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize json request body to type {Type}", nameof(ReceiveServiceNowMessageRequestBody));
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }

        return _createResponse.CreateHttpResponse(HttpStatusCode.Accepted, req);
    }
}
