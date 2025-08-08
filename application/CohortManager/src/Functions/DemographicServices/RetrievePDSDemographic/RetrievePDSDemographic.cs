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
using System.Net.Http.Json;
using System.Collections.Concurrent;

public class RetrievePdsDemographic
{
    private readonly ILogger<RetrievePdsDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly RetrievePDSDemographicConfig _config;
    private readonly IFhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicClient;
    private readonly IBearerTokenService _bearerTokenService;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private const string PdsParticipantUrlFormat = "{0}/{1}";


    public RetrievePdsDemographic(
        ILogger<RetrievePdsDemographic> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig,
        IDataServiceClient<ParticipantDemographic> participantDemographicClient,
        ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue,
        IBearerTokenService bearerTokenService
    )
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _config = retrievePDSDemographicConfig.Value;
        _participantDemographicClient = participantDemographicClient;
        _createBasicParticipantData = createBasicParticipantData;
        _bearerTokenService = bearerTokenService;
        _addBatchToQueue = addBatchToQueue;
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
                var pdsErrorResponse = await response.Content.ReadAsStringAsync();
                await ProcessPdsResponse(response, nhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "PDS returned a 404 please database for details");
            }

            response.EnsureSuccessStatusCode();

            jsonResponse = await _httpClientFunction.GetResponseText(response);
            var pdsDemographic = _fhirPatientDemographicMapper.ParseFhirJson(jsonResponse);
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

    private async Task ProcessPdsResponse(HttpResponseMessage pdsResponse, string nhsNumber)
    {
        var errorResponse = await pdsResponse!.Content.ReadFromJsonAsync<PdsErrorResponse>();
        // we now create a record as an update record and send to the manage participant function. Reason for removal for date should be today and the reason for remove of ORR
        if (errorResponse!.issue!.FirstOrDefault()!.details!.coding!.FirstOrDefault()!.code == "INVALIDATED_RESOURCE")
        {
            var pdsDemographic = new PdsDemographic()
            {
                NhsNumber = nhsNumber,
                PrimaryCareProvider = null,
                ReasonForRemoval = "ORR",
                RemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString("yyyyMMdd")
            };
            var participant = new Participant(pdsDemographic);
            participant.RecordType = Actions.Removed;
            //sends record for an update
            await ProcessRecord(participant);
            return;
        }
        _logger.LogError("the PDS function has returned a 404 error. function now stopping processing");
    }


    private async Task ProcessRecord(Participant participant)
    {
        var updateRecord = new ConcurrentQueue<BasicParticipantCsvRecord>();
        participant.RecordType = participant.RecordType = Actions.Removed;

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            BasicParticipantData = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = "NemsMessages",
            Participant = participant
        };

        updateRecord.Enqueue(basicParticipantCsvRecord);

        _logger.LogInformation("Sending record to the update queue.");
        await _addBatchToQueue.ProcessBatch(updateRecord, _config.ParticipantManagementTopic);
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
}
