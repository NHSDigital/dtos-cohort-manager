namespace NHS.CohortManager.DemographicServices;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.Screening.RetrievePDSDemographic;

public class RetrievePdsDemographic
{
    private readonly ILogger<RetrievePdsDemographic> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly RetrievePDSDemographicConfig _config;
    private readonly IFhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private const string PdsParticipantUrlFormat = "{0}/{1}";

    public RetrievePdsDemographic(
        ILogger<RetrievePdsDemographic> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig)
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _config = retrievePDSDemographicConfig.Value;
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
}
