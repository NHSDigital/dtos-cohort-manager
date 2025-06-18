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
                var Updated = await UpdateDemographicRecordFromPDS(updatedParticipantDemographic);
                if (!Updated) {throw new Exception("Failed to update Demographic record from PDS.");};

                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(demographic));
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

    private async Task<bool> UpdateDemographicRecordFromPDS(ParticipantDemographic updatedParticipantDemographic)
    {
            // Check participant exists in Participant Demographic table.
            ParticipantDemographic oldParticipantDemographic = await _participantDemographicClient.GetSingleByFilter(i => i.NhsNumber == updatedParticipantDemographic.NhsNumber);
            if (oldParticipantDemographic == null) { throw new NullReferenceException("The participant could not be found, when trying to update old Participant from PDS."); }
            updatedParticipantDemographic.ParticipantId = oldParticipantDemographic.ParticipantId;
            bool updated = await _participantDemographicClient.Update(updatedParticipantDemographic);
            if (!updated) { throw new Exception("updating Demographic record from PDS was not successful"); }
            return updated;
    }
}
