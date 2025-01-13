namespace NHS.CohortManager.ParticipantManagementService;

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Common;
using Model;
using DataServices.Client;

public class GetParticipantReferenceData
{
    private readonly ILogger<GetParticipantReferenceData> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IDataServiceClient<GeneCodeLkp> _geneCodeLkpClient;
    private readonly IDataServiceClient<HigherRiskReferralReasonLkp> _higherRiskReferralReasonLkpClient;

    public GetParticipantReferenceData(ILogger<GetParticipantReferenceData> logger, ICreateResponse createResponse, IDataServiceClient<GeneCodeLkp> geneCodeLkpClient, IDataServiceClient<HigherRiskReferralReasonLkp> higherRiskReferralReasonLkpClient)
    {
        _logger = logger;
        _createResponse = createResponse;
        _geneCodeLkpClient = geneCodeLkpClient;
        _higherRiskReferralReasonLkpClient = higherRiskReferralReasonLkpClient;
    }

    /// <summary>
    /// Gets all GeneCodeLkp and HigherRiskReferralReasonLkp data and returns necessary data to Receive Data function (In BI & Analytics project)
    /// To be deleted after historical data is loaded on to platform
    /// </summary>
    [Function("GetParticipantReferenceData")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            var geneCodeLkpResult = await _geneCodeLkpClient.GetAll();
            Dictionary<string, string> geneCodeDescriptions = new Dictionary<string, string>();

            if(geneCodeLkpResult != null && geneCodeLkpResult.Any())
            {
                foreach (var geneCode in geneCodeLkpResult)
                {
                    geneCodeDescriptions.Add(geneCode.GeneCode, geneCode.GeneCodeDescription);
                }
            }

            var higherRiskReferralReasonLkpResult = await _higherRiskReferralReasonLkpClient.GetAll();
            Dictionary<string, string> higherRiskReferralReasonCodeDescriptions = new Dictionary<string, string>();

            if(higherRiskReferralReasonLkpResult != null && higherRiskReferralReasonLkpResult.Any())
            {
                foreach (var higherRiskReferralReasonLkp in higherRiskReferralReasonLkpResult)
                {
                    higherRiskReferralReasonCodeDescriptions.Add(higherRiskReferralReasonLkp.HigherRiskReferralReasonCode, higherRiskReferralReasonLkp.HigherRiskReferralReasonCodeDescription);
                }
            }

            var data = new ParticipantReferenceData(geneCodeDescriptions, higherRiskReferralReasonCodeDescriptions);

            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to get participant reference data");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
