namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Enums;
using NHS.Screening.RetrievePDSDemographic;

public class RetrievePdsDemographic
{
    private readonly ILogger<RetrievePdsDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly RetrievePDSDemographicConfig _config;
    private readonly IFhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
    private const string PdsParticipantUrlFormat = "{0}/{1}";

    public RetrievePdsDemographic(
        ILogger<RetrievePdsDemographic> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig,
        IDataServiceClient<ParticipantDemographic> participantDemographicClient)
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _config = retrievePDSDemographicConfig.Value;
        _participantDemographicClient = participantDemographicClient;
    }

    // TODO: Need to send an exception to the EXCEPTION_MANAGEMENT table whenever this function returns a non OK status.
    [Function("RetrievePdsDemographic")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var nhsNumber = req.Query["nhsNumber"];

        if (string.IsNullOrEmpty(nhsNumber) || !ValidationHelper.ValidateNHSNumber(nhsNumber))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS number provided.");
        }

        try
        {
            var url = string.Format(PdsParticipantUrlFormat, _config.RetrievePdsParticipantURL, nhsNumber);

            var response = await _httpClientFunction.SendPdsGet(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var jsonResponse = await _httpClientFunction.GetResponseText(response);
                var demographic = _fhirPatientDemographicMapper.ParseFhirJson(jsonResponse);
                var updatedParticipantDemographic = demographic.ToParticipantDemographic();
                var updateResult = await UpdateDemographicRecordFromPDS(updatedParticipantDemographic);
                return updateResult switch
                {
                    UpdateResult.Success => CreateSuccessResponse(req, demographic),
                    UpdateResult.NotFound => HandleParticipantNotFound(req, nhsNumber),
                    UpdateResult.UpdateFailed => HandleUpdateFailure(req, nhsNumber),
                    _ => HandleUnexpectedUpdateResult(req)
                };
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error fetching PDS participant data: {Message}", ex.Message);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<UpdateResult> UpdateDemographicRecordFromPDS(ParticipantDemographic updatedParticipantDemographic)
    {
        // Check participant exists in Participant Demographic table.
        ParticipantDemographic oldParticipantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == updatedParticipantDemographic.NhsNumber);
        if (oldParticipantDemographic == null)
        {
            _logger.LogWarning("The participant could not be found, when trying to update old Participant from PDS.");
            return UpdateResult.NotFound;
        }
        updatedParticipantDemographic.ParticipantId = oldParticipantDemographic.ParticipantId;
        bool updateSuccess = await _participantDemographicClient.Update(updatedParticipantDemographic);
        return updateSuccess ? UpdateResult.Success : UpdateResult.UpdateFailed;
    }

    private HttpResponseData CreateSuccessResponse(HttpRequestData req, PdsDemographic demographic)
    {
        return _createResponse.CreateHttpResponse(HttpStatusCode.OK,req,JsonSerializer.Serialize(demographic));
    }

    private HttpResponseData HandleParticipantNotFound(HttpRequestData req, string nhsNumber)
    {
        _logger.LogWarning("Participant not found when updating from PDS for NHS number");
        return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req);
    }

    private HttpResponseData HandleUpdateFailure(HttpRequestData req, string nhsNumber)
    {
        _logger.LogError("Failed to update Demographic record from PDS.");
        return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
    }

    private HttpResponseData HandleUnexpectedUpdateResult(HttpRequestData req)
    {
        _logger.LogError("Unexpected result when updating participant demographic record from PDS.");
        return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
    }

}
