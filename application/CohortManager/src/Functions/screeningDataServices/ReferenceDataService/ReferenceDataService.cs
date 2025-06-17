
namespace ReferenceDataService;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DataServices.Core;
using Model;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

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
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var result = await _gpPracticeRequestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }
    [Function("BsSelectOutCode")]
    public async Task<HttpResponseData> RunBsSelectOutCode([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "BsSelectOutCode/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var result = await _outCodeRequestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }
    [Function("LanguageCode")]
    public async Task<HttpResponseData> RunLanguageCode([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "LanguageCode/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var result = await _languageCodeRequestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }
    [Function("CurrentPosting")]
    public async Task<HttpResponseData> RunCurrentPosting([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "CurrentPosting/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var result = await _currentPostingRequestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }
    [Function("ExcludedSMULookup")]
    public async Task<HttpResponseData> RunExcludedSMU([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "ExcludedSMU/{*key}")] HttpRequestData req, string? key)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var result = await _excludedSMURequestHandler.HandleRequest(req, key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred ");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req, $"An error has occurred {ex.Message}");
        }
    }
}
