namespace NHS.CohortManager.DemographicServices;

using System;
using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

public class RetrievePdsDemographic
{
    private readonly ILogger<RetrievePdsDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly RetrievePDSDemographicConfig _config;
    private readonly IFhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private readonly IBearerTokenService _bearerTokenService;
    private readonly IPdsProcessor _pdsProcessor;
    private const string PdsParticipantUrlFormat = "{0}/{1}";

    public RetrievePdsDemographic(
        ILogger<RetrievePdsDemographic> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig,
        IBearerTokenService bearerTokenService,
        IPdsProcessor pdsProcessor
    )
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _config = retrievePDSDemographicConfig.Value;
        _bearerTokenService = bearerTokenService;
        _pdsProcessor = pdsProcessor;
    }

    // TODO: Need to send an exception to the EXCEPTION_MANAGEMENT table whenever this function returns a non OK status.
    [Function("RetrievePdsDemographic")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            var nhsNumber = req.Query["nhsNumber"];
            string? sourceFileName = null;
            var parsed = QueryHelpers.ParseQuery(req.Url.Query);
            if (parsed.TryGetValue("sourceFileName", out var sv))
            {
                sourceFileName = sv.ToString();
            }

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

            var jsonResponse = await _httpClientFunction.GetResponseText(response);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("PDS returned a 404");
                await _pdsProcessor.ProcessPdsNotFoundResponse(response, nhsNumber, sourceFileName);
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "PDS returned a 404");
            }

            var pdsDemographic = _fhirPatientDemographicMapper.ParseFhirJson(jsonResponse);

            if (pdsDemographic.ConfidentialityCode == "R")
            {
                _logger.LogError("ConfidentialityCode was set to 'R', returning 404");
                return _createResponse.CreateHttpResponse(HttpStatusCode.NotFound, req, "PDS returned a 404");
            }

            response.EnsureSuccessStatusCode();

            var participantDemographic = pdsDemographic.ToParticipantDemographic();
            var upsertResult = await _pdsProcessor.UpsertDemographicRecordFromPDS(participantDemographic);

            return upsertResult ?
                _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(pdsDemographic)) :
                _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been an error retrieving PDS participant data.");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
