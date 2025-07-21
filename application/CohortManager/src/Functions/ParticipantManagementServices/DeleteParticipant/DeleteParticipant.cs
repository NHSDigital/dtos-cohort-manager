namespace NHS.CohortManager.ParticipantManagementService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using Microsoft.Extensions.Logging;
using Model;
using System.Text;
using System.Text.Json;
using DataServices.Client;

public class DeleteParticipant
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<DeleteParticipant> _logger;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;

    public DeleteParticipant(ICreateResponse createResponse, ILogger<DeleteParticipant> logger,
                                IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                IExceptionHandler exceptionHandler)
    {
        _createResponse = createResponse;
        _logger = logger;
        _exceptionHandler = exceptionHandler;
        _cohortDistributionClient = cohortDistributionClient;
    }

    /// <summary>
    /// Azure Function that deletes participant records from the data source
    /// when all specified identifying attributes match (NHS number, family name, and date of birth).
    /// </summary>
    /// <param name="req">
    /// An HTTP request containing a JSON body with participant details:
    /// NHS number (string), family name (string), and date of birth (DateTime).
    /// </param>
    /// <returns>
    /// A 200 OK response if participants were deleted; 404 if none found; 400 or 500 for errors.
    /// </returns>

    [Function("DeleteParticipant")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("DeleteParticipant function called");

        long NhsNumber;
        string? FamilyName;
        DateTime? DateOfBirth;
        DeleteParticipantRequestBody? requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }

            requestBody = JsonSerializer.Deserialize<DeleteParticipantRequestBody>(requestBodyJson);
            if (requestBody == null)
            {
                _logger.LogError("Request body is null");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }

            NhsNumber = long.Parse(requestBody.NhsNumber!);
            FamilyName = requestBody.FamilyName;
            DateOfBirth = requestBody.DateOfBirth;

            if (string.IsNullOrEmpty(NhsNumber.ToString()) || string.IsNullOrEmpty(FamilyName) || !requestBody.DateOfBirth.HasValue)
            {
                _logger.LogError("Please ensure that all parameters are not null or empty");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Serialization failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            var participantData = await _cohortDistributionClient.GetByFilter(p => p.NHSNumber == NhsNumber && p.FamilyName == FamilyName);

            var participantsToDelete = participantData.Where(p => p.DateOfBirth == DateOfBirth);
            if (!participantsToDelete.Any())
            {
                _logger.LogInformation("No participants found with the specified parameters");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No participants found with the specified parameters");
            }

            foreach (var participant in participantsToDelete)
            {
                await _cohortDistributionClient.Delete(participant.CohortDistributionId.ToString());
            }

            _logger.LogInformation("Deleted participants");

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber?.ToString() ?? "N/A", "", "", JsonSerializer.Serialize(requestBody) ?? "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    /// <summary>
    /// Azure Function endpoint that previews participant data matching all specified attributes
    /// before deletion.
    /// </summary>
    /// <param name="req">
    /// An HTTP request containing NHS number, family name, and date of birth.
    /// </param>
    /// <returns>
    /// A 200 OK response with participant data if found; 404 if no match; 400 or 500 for errors.
    /// </returns>

    [Function("PreviewParticipant")]
    public async Task<HttpResponseData> PreviewAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("Preview Participant Called");

        string requestBodyJson;
        DeleteParticipantRequestBody? requestBody;

        try
        {
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }

            requestBody = JsonSerializer.Deserialize<DeleteParticipantRequestBody>(requestBodyJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize request body.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid request format.");
        }

        if (requestBody == null ||
            string.IsNullOrWhiteSpace(requestBody.NhsNumber) ||
            string.IsNullOrWhiteSpace(requestBody.FamilyName) ||
            !requestBody.DateOfBirth.HasValue)
        {
            _logger.LogError("Missing one or more required parameters.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid or missing parameters.");
        }

        try
        {
            var nhsNumber = long.Parse(requestBody.NhsNumber);
            var familyName = requestBody.FamilyName;
            var dateOfBirth = requestBody.DateOfBirth;

            var participantData = await _cohortDistributionClient.GetByFilter(p =>
                p.NHSNumber == nhsNumber &&
                p.FamilyName == familyName
            );

            var matchingParticipants = participantData.Where(p => p.DateOfBirth == dateOfBirth);

            if (!matchingParticipants.Any())
            {
                _logger.LogInformation("No matching participants found");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "No matching records found.");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(matchingParticipants));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview failed: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "An error occurred while retrieving preview data.");
        }
    }
}

