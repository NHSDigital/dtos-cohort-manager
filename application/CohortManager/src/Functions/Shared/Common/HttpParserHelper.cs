namespace Common;

using System.Net;
using Common.Interfaces;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class HttpParserHelper : IHttpParserHelper
{
    private readonly ILogger<HttpParserHelper> _logger;
    private readonly ICreateResponse _createResponse;
    public HttpParserHelper(ILogger<HttpParserHelper> logger, ICreateResponse createResponse)
    {
        _logger = logger;
        _createResponse = createResponse;
    }
    public int GetQueryParameterAsInt(HttpRequestData req, string key)
    {
        var defaultValue = 0;
        var queryString = req.Query[key];
        return int.TryParse(queryString, out int value) ? value : defaultValue;
    }
    public HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage)
    {
        _logger.LogError(errorMessage);
        return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, errorMessage);
    }

    public bool GetQueryParameterAsBool(HttpRequestData req, string key, bool defaultValue = false)
    {
        var queryString = req.Query[key];

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return defaultValue;
        }

        switch (queryString.ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "y":
                return true;
            case "false":
            case "0":
            case "no":
            case "n":
                return false;
            default:
                return defaultValue;
        }
    }

    public Demographic FhirParser(string json)
    {
        var parser = new FhirJsonParser();

        try
        {
            var parsedPatient = parser.Parse<Patient>(json);
            return new Demographic(parsedPatient);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to parse FHIR json");
            throw;
        }
    }
}
