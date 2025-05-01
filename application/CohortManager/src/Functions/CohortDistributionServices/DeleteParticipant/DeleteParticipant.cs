namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using Microsoft.Extensions.Logging;
using Model;
using System.Text;
using System.Text.Json;
using DataServices.Client;
using Microsoft.Extensions.Options;
using NHS.Screening.DeleteParticipant;

public class DeleteParticipant
{
    private readonly ICreateResponse _createResponse;
    private readonly ILogger<DeleteParticipant> _logger;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<CohortDistribution> _cohortDistributionClient;
    private readonly DeleteParticipantConfig _config;

    public DeleteParticipant(ICreateResponse createResponse, ILogger<DeleteParticipant> logger,
                                IDataServiceClient<CohortDistribution> cohortDistributionClient,
                                IExceptionHandler exceptionHandler,
                                IOptions<DeleteParticipantConfig> DeleteParticipantConfig)
    {
        _createResponse = createResponse;
        _logger = logger;
        _exceptionHandler = exceptionHandler;
        _cohortDistributionClient = cohortDistributionClient;
        _config = DeleteParticipantConfig.Value;
    }

    [Function("DeleteParticipant")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        long NhsNumber;
        string? FamilyName;
        DateTime? DateOfBirth;
        DeleteParticipantRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = await reader.ReadToEndAsync();
            }

            requestBody = JsonSerializer.Deserialize<DeleteParticipantRequestBody>(requestBodyJson);
            NhsNumber= long.Parse(requestBody.NhsNumber);
            FamilyName = requestBody.FamilyName;
            DateOfBirth = requestBody.DateOfBirth;

            if (string.IsNullOrEmpty(NhsNumber.ToString()) || string.IsNullOrEmpty(FamilyName) || !requestBody.DateOfBirth.HasValue)
            {
                _logger.LogError("Invalid request body");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            }


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
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
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber.ToString(), "", "", JsonSerializer.Serialize(requestBody) ?? "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}

public class DeleteParticipantRequestBody
{
    public string? NhsNumber { get; set; }
    public string? FamilyName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
