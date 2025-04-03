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
    private readonly IHttpParserHelper _httpParserHelper;

    public RetrievePdsDemographic(
        ILogger<RetrievePdsDemographic> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IHttpParserHelper httpParserHelper,
        IOptions<RetrievePDSDemographicConfig> retrievePDSDemographicConfig)
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _httpParserHelper = httpParserHelper;
        _config = retrievePDSDemographicConfig.Value;
    }

    [Function("RetrievePdsDemographic")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var nhsNumber = req.Query["nhsNumber"];

        if (string.IsNullOrEmpty(nhsNumber))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "No NHS number provided.");
        }

        if (!ValidationHelper.ValidateNHSNumber(nhsNumber))
        {
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS number provided.");
        }

        try
        {
            var url = $"{_config.RetrievePdsParticipantURL}/{nhsNumber}";

            var headers = new Dictionary<string, string>()
            {
                {"X-Request-ID", Guid.NewGuid().ToString() },
                {"X-Correlation-ID", Guid.NewGuid().ToString() },
                {"Accept", "application/fhir+json" },
            };

            var response = await _httpClientFunction.GetAsync(url, headers);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var demographic = _httpParserHelper.FhirParser(jsonResponse);
                Console.WriteLine(JsonSerializer.Serialize(demographic));

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
