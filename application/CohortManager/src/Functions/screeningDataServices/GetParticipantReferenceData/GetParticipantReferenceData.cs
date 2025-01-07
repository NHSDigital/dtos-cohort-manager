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
    private readonly IParticipantManagerData _participantManagerData;
    private readonly IExceptionHandler _handleException;
    private readonly ICallFunction _callFunction;

    private readonly ICreateCohortDistributionData _createCohortDistributionData;

    public GetParticipantReferenceData(ILogger<UpdateParticipantDetails> logger, ICreateResponse createResponse, IParticipantManagerData participantManagerData, IExceptionHandler handleException, ICallFunction callFunction, ICreateCohortDistributionData createCohortDistributionData)
    {
        _logger = logger;
        _createResponse = createResponse;
        _participantManagerData = participantManagerData;
        _handleException = handleException;
        _callFunction = callFunction;
        _createCohortDistributionData = createCohortDistributionData;
    }

    [Function("GetParticipantReferenceData")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var participantCsvRecord = new ParticipantCsvRecord();

        var geneCodeLkpResult = await _geneCodeLkpClient.GetByFilter(i => 1 == 1);
        List<GeneCodeLkp> geneCodes = new List<GeneCodeLkp>();

        if(geneCodeLkpResult != null && geneCodeLkpResult.Any())
        {
            geneCode = geneCodeLkpResult.ToList();
        }

        try
        {
            string requestBody = "";

            using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
                participantCsvRecord = JsonSerializer.Deserialize<ParticipantCsvRecord>(requestBody);
            }

            var existingParticipantData = _participantManagerData.GetParticipant(participantCsvRecord.Participant.NhsNumber, participantCsvRecord.Participant.ScreeningId);

            var response = await ValidateData(existingParticipantData, participantCsvRecord.Participant, participantCsvRecord.FileName);
            if (response.IsFatal)
            {
                _logger.LogError("Validation Error: A fatal Rule was violated and therefore the record cannot be added to the database with Nhs number: {NhsNumber}", participantCsvRecord.Participant.NhsNumber);
                return _createResponse.CreateHttpResponse(HttpStatusCode.Created, req);
            }

            if (response.CreatedException)
            {
                _logger.LogInformation("Validation Error: A Rule was violated but it was not Fatal for record with Nhs number: {NhsNumber}", participantCsvRecord.Participant.NhsNumber);
                participantCsvRecord.Participant.ExceptionFlag = "Y";
            }

            var isAdded = _participantManagerData.UpdateParticipantDetails(participantCsvRecord);

            if (isAdded)
            {
                return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req);
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
    private async Task<ValidationExceptionLog> ValidateData(Participant existingParticipant, Participant newParticipant, string fileName)
    {
        var json = JsonSerializer.Serialize(new LookupValidationRequestBody(existingParticipant, newParticipant, fileName, Model.Enums.RulesType.ParticipantManagement));

        try
        {
            var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("LookupValidationURL"), json);
            var responseBodyJson = await _callFunction.GetResponseText(response);
            var responseBody = JsonSerializer.Deserialize<ValidationExceptionLog>(responseBodyJson);

            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, $"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {newParticipant}");
            return null;
        }
    }
}
