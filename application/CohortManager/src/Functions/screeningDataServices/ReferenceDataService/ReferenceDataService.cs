
namespace ReferenceDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DataServices.Core;
using Model;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

/// <summary>
/// Reference Data Service
/// This function exposes a number of endpoints for Reference data used by the Lookup Validation Rules
/// </summary>
public class ReferenceDataService
{
    private readonly ILogger<ReferenceDataService> _logger;
    private readonly IRequestHandler<BsSelectGpPractice> _gpPracticeRequestHandler;
    private readonly IRequestHandler<BsSelectOutCode> _outCodeRequestHandler;
    private readonly IRequestHandler<LanguageCode> _languageCodeRequestHandler;
    private readonly IRequestHandler<CurrentPosting> _currentPostingRequestHandler;
    private readonly IRequestHandler<ExcludedSMULookup> _excludedSMURequestHandler;
    private readonly ICreateResponse _createResponse;

    public ReferenceDataService(
        ILogger<ReferenceDataService> logger,
        IRequestHandler<BsSelectGpPractice> gpPracticeRequestHandler,
        IRequestHandler<BsSelectOutCode> outCodeRequestHandler,
        IRequestHandler<LanguageCode> languageCodeRequestHandler,
        IRequestHandler<CurrentPosting> currentPostingRequestHandler,
        IRequestHandler<ExcludedSMULookup> excludedSMURequestHandler,
        ICreateResponse createResponse
    )
    {
        _logger = logger;
        _gpPracticeRequestHandler = gpPracticeRequestHandler;
        _outCodeRequestHandler = outCodeRequestHandler;
        _languageCodeRequestHandler = languageCodeRequestHandler;
        _currentPostingRequestHandler = currentPostingRequestHandler;
        _excludedSMURequestHandler = excludedSMURequestHandler;
        _createResponse = createResponse;
    }

    [Function("BsSelectGpPractice")]
    public async Task<HttpResponseData> RunBsSelectGpPractice([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "BsSelectGpPractice/{*key}")] HttpRequestData req, string? key)
    {
        return await RunHandlerAsync(_gpPracticeRequestHandler.HandleRequest, req, key);
    }
    [Function("BsSelectOutCode")]
    public async Task<HttpResponseData> RunBsSelectOutCode([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "BsSelectOutCode/{*key}")] HttpRequestData req, string? key)
    {
        return await RunHandlerAsync(_outCodeRequestHandler.HandleRequest, req, key);
    }
    [Function("LanguageCode")]
    public async Task<HttpResponseData> RunLanguageCode([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "LanguageCode/{*key}")] HttpRequestData req, string? key)
    {
        return await RunHandlerAsync(_languageCodeRequestHandler.HandleRequest, req, key);
    }

    [Function("CurrentPosting")]
    public async Task<HttpResponseData> RunCurrentPosting([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "CurrentPosting/{*key}")] HttpRequestData req, string? key)
    {
        return await RunHandlerAsync(_currentPostingRequestHandler.HandleRequest, req, key);
    }

    [Function("ExcludedSMULookup")]
    public async Task<HttpResponseData> RunExcludedSMU([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "ExcludedSMU/{*key}")] HttpRequestData req, string? key)
    {
        return await RunHandlerAsync(_excludedSMURequestHandler.HandleRequest, req, key);
    }

    private async Task<HttpResponseData> RunHandlerAsync(Func<HttpRequestData, string?, Task<HttpResponseData>> handler, HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var result = await handler(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }
}
