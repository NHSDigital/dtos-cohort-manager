using System.Net;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GetValidationExceptions
{
    public class GetValidationExceptions
    {
        private readonly ILogger<GetValidationExceptions> _logger;
        private readonly ICreateResponse _createResponse;

        private readonly IValidationData _validationData;

        public GetValidationExceptions(ILogger<GetValidationExceptions> logger, ICreateResponse createResponse, IValidationData validationData)
        {
            _logger = logger;
            _createResponse = createResponse;
            _validationData = validationData;
        }

        [Function("GetValidationExceptions")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            foreach (var ruleBroken in _validationData.GetAllBrokenRules())
            {
                _logger.LogInformation($"rule broken {ruleBroken.Rule}");
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
        }
    }
}
