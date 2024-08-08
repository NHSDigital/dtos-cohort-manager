using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using NHS.CohortManager.CohortDistribution;

namespace GetLatestCohortDistributionRecord
{
    public class GetLatestCohortDistributionRecordData
    {
        private readonly ILogger<GetLatestCohortDistributionRecordData> _logger;
        private readonly ICreateResponse _createResponse;
        private readonly ICreateCohortDistributionData _createCohortDistributionData;

        private readonly IExceptionHandler _exceptionHandler;


        public GetLatestCohortDistributionRecordData(ILogger<GetLatestCohortDistributionRecordData> logger, ICreateResponse createResponse, ICreateCohortDistributionData createCohortDistributionData, IExceptionHandler exceptionHandler)
        {
            _createResponse = createResponse;
            _createCohortDistributionData = createCohortDistributionData;
            _exceptionHandler = exceptionHandler;
            _logger = logger;
        }

        [Function("GetLatestCohortDistributionRecord")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var requestBody = new CreateCohortDistributionRequestBody();
            try
            {
                string requestBodyJson;
                using (var reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    requestBodyJson = reader.ReadToEnd();
                }

                requestBody = JsonSerializer.Deserialize<CreateCohortDistributionRequestBody>(requestBodyJson);
            }
            catch (Exception ex)
            {
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName);

                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            try
            {
                var lastParticipant = _createCohortDistributionData.GetLastCohortDistributionParticipant(requestBody.NhsNumber);
                var LasParticipantJson = JsonSerializer.Serialize<CohortDistributionParticipant>(lastParticipant);
                if (lastParticipant != null)
                {

                    return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, LasParticipantJson);
                }
                _logger.LogError("there are no items for this nhs number in the cohort distribution table");
                return _createResponse.CreateHttpResponse(HttpStatusCode.BadRequest, req, LasParticipantJson);
            }
            catch (Exception ex)
            {
                await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, requestBody.NhsNumber, requestBody.FileName);
                _logger.LogError($"there was an error while getting the latest two items from the database {ex.Message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }
}
