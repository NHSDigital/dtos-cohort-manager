namespace Common.Interfaces;

using Microsoft.Azure.Functions.Worker.Http;
using Model;

public interface IHttpParserHelper
{
    HttpResponseData LogErrorResponse(HttpRequestData req, string errorMessage);
    int GetQueryParameterAsInt(HttpRequestData req, string key);
    bool GetQueryParameterAsBool(HttpRequestData req, string key, bool defaultValue = false);

    /// <summary>
    /// Parses FHIR response json and maps it to the Demographic model.
    /// </summary>
    /// <param name="json">Raw FHIR response json from PDS.</param>
    /// <returns>Demographic</returns>
    Demographic FhirParser(string json);
};
