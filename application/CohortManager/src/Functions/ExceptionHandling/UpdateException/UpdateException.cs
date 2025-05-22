namespace NHS.CohortManager.ExceptionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using Model;
using Common;
using DataServices.Client;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

public class UpdateException
{
    private readonly ILogger<UpdateException> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IDataServiceClient<ExceptionManagement> _exceptionManagementDataService;

    public UpdateException(
        ILogger<UpdateException> logger,
        IDataServiceClient<ExceptionManagement> exceptionManagementDataService,
        ICreateResponse createResponse)
    {
        _logger = logger;
        _exceptionManagementDataService = exceptionManagementDataService;
        _createResponse = createResponse;
    }

    [Function("UpdateException")]
    [OpenApiOperation(operationId: "UpdateExceptionRecord",
        Summary = "Updates an exception record with ServiceNow information",
        Description = "Updates an existing exception record with ServiceNow ticket number and timestamp",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(UpdateExceptionRequest),
        Description = "The exception update request containing ServiceNow number",
        Required = true)]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(string),
        Description = "Successfully updated the exception record")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.BadRequest,
        contentType: "application/json",
        bodyType: typeof(string),
        Description = "Invalid ExceptionId provided.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NoContent,
        contentType: "application/json",
        bodyType: typeof(string),
        Description = "No exception found with the supplied exception id")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(string),
        Description = "Internal server error occurred")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequestData req)
    {
        _logger.LogInformation("Processing request to update ServiceNow number in exception management table.");

        try
        {
            var requestBody = await ReadRequestBodyAsync(req);
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Request body is empty.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
            }

            var updateRequest = DeserializeRequest(requestBody);
            if (updateRequest == null || !int.TryParse(updateRequest.ExceptionId, out int exceptionId) || exceptionId == 0)
            {
                _logger.LogWarning("Invalid ExceptionId provided.");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            // 1. Check & Fetch Exception Record
            var exceptionData = await _exceptionManagementDataService.GetSingleByFilter(x => x.ExceptionId == exceptionId);
            if (exceptionData == null)
            {
                _logger.LogWarning("No exception found with ID: {ExceptionId}", exceptionId);
                return _createResponse.CreateHttpResponse(HttpStatusCode.NoContent, req);
            }

            // 2. Update Exception Record with ServiceNow number.
            UpdateExceptionRecord(exceptionData, updateRequest);

            var updateSuccess = await _exceptionManagementDataService.Update(exceptionData);
            if (!updateSuccess)
            {
                _logger.LogError("Failed to update exception: {Exception}", exceptionData);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            _logger.LogInformation("Exception record updated with servicenow number successfully.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            //return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "", new ErrorResponse { Message = "An unexpected error occurred", Details = ex.Message });
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static UpdateExceptionRequest? DeserializeRequest(string requestBody)
    {
        try
        {
            return JsonSerializer.Deserialize<UpdateExceptionRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static void UpdateExceptionRecord(ExceptionManagement exceptionData, UpdateExceptionRequest updateRequest)
    {
        exceptionData.ServiceNowId = updateRequest.ServiceNowNumber; // can be null
        exceptionData.ServiceNowCreatedDate = DateTime.Now;
    }
}

