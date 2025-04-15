namespace NHS.CohortManager.CohortDistributionService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using System.Net;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;
using System.Text;
using System.Text.Json;
using DataServices.Client;
using Microsoft.Extensions.Options;
using NHS.Screening.DeleteParticipant;
using System.Globalization;

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
            FamilyName = requestBody.FamilyName;
            DateOfBirth = requestBody.DateOfBirth;

            // if (string.IsNullOrEmpty(FamilyName) || DateOfBirth == null)
            // {
            //     _logger.LogError("Invalid request body");
            //     return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
            // }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req);
        }

        try
        {
            long longNhsNumber = long.Parse(requestBody.NhsNumber);
            var participantData = await _cohortDistributionClient.GetByFilter(p => p.NHSNumber == longNhsNumber && p.DateOfBirth == DateOfBirth);
                                                                            // p.DateOfBirth == DateOfBirth &&
                                                                            // p.FamilyName == FamilyName);
            if (participantData == null)
            {
                _logger.LogError("The participantData was null the {DeleteParticipant}  function", nameof(DeleteParticipant));

                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, $"the participantData was null the {nameof(DeleteParticipant)}  function");
            }

            // var deleteParticipant = await _cohortDistributionClient.Delete(participantData.CohortDistributionId.ToString());
            // if (!deleteParticipant)
            // {
            //     _logger.LogError("Failed to delete participant with NHS Number: {NHSNumber}", participantData.NHSNumber);
            //     return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "Failed to delete participant");
            // }

            // _logger.LogInformation("Deleted participant with NHS Number: {NHSNumber}", participantData.NHSNumber);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete participant function failed.\nMessage: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, "", "", JsonSerializer.Serialize(requestBody) ?? "N/A");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
