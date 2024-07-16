namespace NHS.CohortManager.CohortDistributionDataServices
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Common;
    using Common.Interfaces;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;
    using Model;

    public class AddCohortDistributionDataFunction
    {
        private readonly ILogger<AddCohortDistributionDataFunction> _logger;
        private readonly ICreateResponse _createResponse;
        private readonly ICreateCohortDistributionData _createCohortDistributionData;

        public AddCohortDistributionDataFunction(ILogger<AddCohortDistributionDataFunction> logger, ICreateCohortDistributionData createCohortDistributionData, ICreateResponse createResponse)
        {
            _logger = logger;
            _createCohortDistributionData = createCohortDistributionData;
            _createResponse = createResponse;
        }

        [Function("AddCohortDistributionData")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            try
            {
                string requestBody = "";
                var participantCsvRecord = new CohortDistributionParticipant();
                using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                    participantCsvRecord = JsonSerializer.Deserialize<CohortDistributionParticipant>(requestBody);
                }

                var isAdded = _createCohortDistributionData.InsertCohortDistributionData(participantCsvRecord);
                if (isAdded)
                {
                    return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
                }

                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
            }
        }
    }
}
