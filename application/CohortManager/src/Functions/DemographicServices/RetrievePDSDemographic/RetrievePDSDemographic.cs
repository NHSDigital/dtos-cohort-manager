namespace NHS.CohortManager.DemographicServices;

using System;
using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Model;

public class RetrievePdsDemographic
{
    private readonly ILogger<RetrievePdsDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly RetrievePDSDemographicConfig _config;
    private readonly IFhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
    private readonly IBearerTokenService _bearerTokenService;
    private const string PdsParticipantUrlFormat = "{0}/{1}";


    public RetrievePdsDemographic(
        ILogger<RetrievePdsDemographic> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig,
        IDataServiceClient<ParticipantDemographic> participantDemographicClient,
        IBearerTokenService bearerTokenService
    )
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _config = retrievePDSDemographicConfig.Value;
        _participantDemographicClient = participantDemographicClient;
        _bearerTokenService = bearerTokenService;
    }

    // TODO: Need to send an exception to the EXCEPTION_MANAGEMENT table whenever this function returns a non OK status.
    [Function("RetrievePdsDemographic")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            var nhsNumber = req.Query["nhsNumber"];

            var bearerToken = await _bearerTokenService.GetBearerToken();
            if (bearerToken == null)
            {
                _logger.LogError("the bearer token could not be found");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, "The bearer token could not be found");
            }

            if (string.IsNullOrEmpty(nhsNumber) || !ValidationHelper.ValidateNHSNumber(nhsNumber))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS number provided.");
            }


            var url = string.Format(PdsParticipantUrlFormat, _config.RetrievePdsParticipantURL, nhsNumber);
            var response = await _httpClientFunction.SendPdsGet(url, bearerToken);
            string jsonResponse = "";

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var demographicRecordDeletedFromDatabase = await DeleteDemographicRecord(nhsNumber);
                if (!demographicRecordDeletedFromDatabase)
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "could not delete record from database. See logs for more details.");
                }
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "Record not found in PDS and successfully removed from cohort manager database.");
            }

            response.EnsureSuccessStatusCode();

            jsonResponse = await _httpClientFunction.GetResponseText(response);
            var pdsDemographic = _fhirPatientDemographicMapper.ParseFhirJson(jsonResponse);

            if (pdsDemographic.ConfidentialityCode == "R")
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "Demographic data is restricted in PDS");
            }

            var participantDemographic = pdsDemographic.ToParticipantDemographic();
            var upsertResult = await UpsertDemographicRecordFromPDS(participantDemographic);

            return upsertResult ?
                _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(participantDemographic)) :
                _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error retrieving PDS participant data.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private async Task<bool> UpsertDemographicRecordFromPDS(ParticipantDemographic participantDemographic)
    {
        ParticipantDemographic oldParticipantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == participantDemographic.NhsNumber);

        if (oldParticipantDemographic == null)
        {
            _logger.LogInformation("Participant Demographic record not found, attemping to add Participant Demographic.");
            bool addSuccess = await _participantDemographicClient.Add(participantDemographic);

            if (addSuccess)
            {
                _logger.LogInformation("Successfully added Participant Demographic.");
                return true;
            }

            _logger.LogError("Failed to add Participant Demographic.");
            return false;
        }

        _logger.LogInformation("Participant Demographic record found, attempting to update Participant Demographic.");
        participantDemographic.ParticipantId = oldParticipantDemographic.ParticipantId;
        bool updateSuccess = await _participantDemographicClient.Update(participantDemographic);

        if (updateSuccess)
        {
            _logger.LogInformation("Successfully updated Participant Demographic.");
            return true;
        }

        _logger.LogError("Failed to update Participant Demographic.");
        return false;
    }

    private async Task<bool> DeleteDemographicRecord(string nhsNumber)
    {
        var nhsNumberParsed = long.TryParse(nhsNumber, out long parsedNhsNumber);
        if (!nhsNumberParsed)
        {
            _logger.LogError("could not parse nhs number when trying to get record for deletion.");
            return false;
        }
        var oldParticipantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == parsedNhsNumber);

        if (oldParticipantDemographic == null)
        {

            _logger.LogWarning("Failed to delete Participant Demographic as record did not exist in database.");
            return false;
        }

        _logger.LogInformation("Participant Demographic record found, attempting to delete  Participant Demographic.");
        bool updateSuccess = await _participantDemographicClient.Delete(oldParticipantDemographic.ParticipantId.ToString());

        if (updateSuccess)
        {
            _logger.LogInformation("Successfully deleted Participant Demographic.");
            return true;
        }

        _logger.LogError("Failed to delete Participant Demographic.");
        return false;
    }


}
