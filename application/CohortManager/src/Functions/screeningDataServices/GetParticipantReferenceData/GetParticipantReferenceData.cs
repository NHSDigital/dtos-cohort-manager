namespace NHS.CohortManager.ScreeningDataServices;

using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Data.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;

public class GetParticipantReferenceData
{
    private readonly ILogger<UpdateParticipantDetails> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;

    public GetParticipantReferenceData(ILogger<UpdateParticipantDetails> logger, ICreateResponse createResponse, IExceptionHandler handleException, ICallFunction callFunction, ICreateCohortDistributionData createCohortDistributionData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _handleException = handleException;
        _callFunction = callFunction;
    }

    [Function("GetParticipantReferenceData")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        try
        {
            var geneCodeLkpResult = await _geneCodeLkpClient.GetByFilter(i => 1 == 1);
            List<GeneCodeLkp> geneCodes = new List<GeneCodeLkp>();

            if(geneCodeLkpResult != null && geneCodeLkpResult.Any())
            {
                geneCodes = geneCodeLkpResult.ToList();
            }

            var higherRiskReferralReasonLkpResult = await _higherRiskReferralReasonLkpClient.GetByFilter(i => 1 == 1);
            List<HigherRiskReferralReasonLkp> higherRiskReferralReasonLkps = new List<HigherRiskReferralReasonLkp>();

            if(higherRiskReferralReasonLkpResult != null && higherRiskReferralReasonLkpResult.Any())
            {
                higherRiskReferralReasonLkps = higherRiskReferralReasonLkpResult.ToList();
            }

            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message, ex);
            await _handleException.CreateSystemExceptionLog(ex, participantCsvRecord.Participant, participantCsvRecord.FileName);
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }
}
