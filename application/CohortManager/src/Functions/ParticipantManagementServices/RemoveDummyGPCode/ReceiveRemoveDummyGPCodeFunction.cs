namespace NHS.CohortManager.ParticipantManagementServices;

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Model.Constants;
using NHS.CohortManager.ParticipantManagementServices.Models;

public class ReceiveRemoveDummyGpCodeFunction
{
    private readonly ILogger<ReceiveRemoveDummyGpCodeFunction> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IQueueClient _queueClient;
    private readonly RemoveDummyGpCodeConfig _config;

    private static readonly Regex NonLetterRegex = new(@"[^\p{Lu}\p{Ll}\p{Lt}]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public ReceiveRemoveDummyGpCodeFunction(
        ILogger<ReceiveRemoveDummyGpCodeFunction> logger,
        ICreateResponse createResponse,
        IHttpClientFunction httpClientFunction,
        IQueueClient queueClient,
        IOptions<RemoveDummyGpCodeConfig> config)
    {
        _logger = logger;
        _createResponse = createResponse;
        _httpClientFunction = httpClientFunction;
        _queueClient = queueClient;
        _config = config.Value;
    }

    /// <summary>
    /// Validates and enqueues a dummy GP code removal request to the ServiceNow participant management topic.
    /// </summary>
    [Function("ReceiveRemoveDummyGPCodeFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "RemoveDummyGPCode")] HttpRequestData req)
    {
        try
        {
            var requestBody = await JsonSerializer.DeserializeAsync<RemoveDummyGPCodeRequestBody>(req.Body);
            if (requestBody == null)
            {
                _logger.LogError("Request body deserialised to null");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Patient not found");
            }

            var validationContext = new ValidationContext(requestBody);
            var validationResult = new List<ValidationResult>();
            var isRequestValid = Validator.TryValidateObject(requestBody, validationContext, validationResult, true);

            if (!isRequestValid)
            {
                _logger.LogError("Request body failed validation");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Patient not found");
            }

            if (!ValidationHelper.ValidateNHSNumber(requestBody.NhsNumber))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Invalid NHS Number");
            }

            var pdsResponse = await _httpClientFunction.SendGetResponse($"{_config.RetrievePdsDemographicURL}?nhsNumber={requestBody.NhsNumber}");

            if (pdsResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Patient not found");
            }

            if (pdsResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Unexpected PDS response status code {StatusCode}", pdsResponse.StatusCode);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            var pdsDemographic = await pdsResponse.Content.ReadFromJsonAsync<PdsDemographic>();
            if (pdsDemographic == null)
            {
                _logger.LogError("Failed to deserialize PDS demographic response");
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            if (!CheckParticipantDataMatches(requestBody, pdsDemographic))
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Patient not found");
            }

            if (!DateOnly.TryParse(pdsDemographic.DateOfBirth, out var pdsDateOfBirth))
            {
                _logger.LogError("PDS demographic date of birth was missing or invalid for NHS Number {NhsNumber}", requestBody.NhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            var participant = new ServiceNowParticipant
            {
                ServiceNowCaseNumber = requestBody.RequestId,
                ScreeningId = 1,
                NhsNumber = long.Parse(requestBody.NhsNumber),
                FirstName = pdsDemographic.FirstName ?? string.Empty,
                FamilyName = pdsDemographic.FamilyName ?? string.Empty,
                DateOfBirth = pdsDateOfBirth,
                BsoCode = pdsDemographic.CurrentPosting ?? string.Empty,
                ReasonForAdding = ServiceNowReasonsForAdding.DummyGpCodeRemoval,
                RequiredGpCode = null
            };

            var enqueueResult = await _queueClient.AddAsync(participant, _config.ServiceNowParticipantManagementTopic);
            if (!enqueueResult)
            {
                _logger.LogError("Failed to enqueue remove dummy GP code request for NHS Number {NhsNumber}", requestBody.NhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.Accepted, req, "Manual GP Code Removal Enqueued");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize request body");
            return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, "Patient not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in ReceiveRemoveDummyGPCodeFunction");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private static bool CheckParticipantDataMatches(RemoveDummyGPCodeRequestBody requestBody, PdsDemographic pdsDemographic)
    {
        return NormalizedNamesMatch(requestBody.Forename, pdsDemographic.FirstName) &&
               NormalizedNamesMatch(requestBody.Surname, pdsDemographic.FamilyName) &&
               requestBody.DateOfBirth.ToString("yyyy-MM-dd") == pdsDemographic.DateOfBirth;
    }

    /// <summary>
    /// Normalizes and compares two name strings by removing accents, spaces, hyphens, and special characters.
    /// Converts accented characters to their base forms (É→E, Ñ→N, Ö→O) to match database storage behavior.
    /// </summary>
    /// <param name="name1">First name to compare</param>
    /// <param name="name2">Second name to compare</param>
    /// <returns>True if the normalized names match (case-insensitive), false otherwise</returns>
    private static bool NormalizedNamesMatch(string? name1, string? name2)
    {
        if (string.IsNullOrWhiteSpace(name1) && string.IsNullOrWhiteSpace(name2))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
        {
            return false;
        }

        var normalized1 = NormalizeName(name1);
        var normalized2 = NormalizeName(name2);

        if (string.IsNullOrEmpty(normalized1) || string.IsNullOrEmpty(normalized2))
        {
            return false;
        }

        return string.Equals(normalized1, normalized2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a name by removing accents and all non-letter characters.
    /// This handles spaces, hyphens, apostrophes, and other punctuation.
    /// Accented characters like É, Ñ, Ö are converted to their base forms (E, N, O).
    /// Uses Unicode NFD normalization to decompose accents, then removes diacritical marks.
    /// </summary>
    /// <param name="name">The name to normalize</param>
    /// <returns>Normalized name containing only unaccented ASCII letters</returns>
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmedName = name.Trim();
        var normalizedString = trimmedName.Normalize(NormalizationForm.FormD);
        var lettersOnlyString = NonLetterRegex.Replace(normalizedString, string.Empty);

        return lettersOnlyString.Normalize(NormalizationForm.FormC);
    }
}
