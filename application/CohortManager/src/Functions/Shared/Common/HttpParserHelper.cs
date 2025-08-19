namespace Common;

using System.Net;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Parses an enum query parameter if it exists. If not, it will return the provided default value.
    /// </summary>
    /// <typeparam name="T">The enum type to parse</typeparam>
    /// <param name="req">The HTTP request data</param>
    /// <param name="key">The query parameter key name</param>
    /// <param name="defaultValue">The default value to return if parsing fails or the parameter is missing</param>
    /// <returns>The parsed enum value or the default value</returns>
    public static T GetEnumQueryParameter<T>(HttpRequestData req, string key, T defaultValue) where T : struct, Enum
    {
        var queryString = req.Query[key];
        if (string.IsNullOrEmpty(queryString))
        {
            return defaultValue;
        }

        return Enum.TryParse<T>(queryString, true, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Parses a DateTime query parameter from the request. Supports various date formats.
    /// </summary>
    /// <param name="req">The HTTP request data</param>
    /// <param name="key">The query parameter key name</param>
    /// <returns>The parsed DateTime value or null if parsing fails or parameter is missing</returns>
    public DateTime? GetQueryParameterAsDateTime(HttpRequestData req, string key)
    {
        var queryString = req.Query[key];

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        string[] formats = {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "MM/dd/yyyy",
        "MM-dd-yyyy",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-dd HH:mm:ss"
    };

        if (DateTime.TryParse(queryString, out DateTime result))
        {
            return result;
        }

        if (DateTime.TryParseExact(queryString, formats, null, System.Globalization.DateTimeStyles.None, out result))
        {
            return result;
        }

        _logger.LogWarning("Failed to parse date parameter '{Key}' with value '{Value}'", key, queryString);
        return null;
    }
}
