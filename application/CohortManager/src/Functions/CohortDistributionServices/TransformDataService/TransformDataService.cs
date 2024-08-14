namespace NHS.CohortManager.CohortDistribution;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using RulesEngine.Models;
using System.Net;
using System.Text;
using Model;
using Model.Enums;
using Common;
using Microsoft.Extensions.Logging;

public class TransformDataService
{
    private readonly ILogger<TransformDataService> _logger;
    private readonly ICreateResponse _createResponse;
    private readonly IExceptionHandler _exceptionHandler;
    public TransformDataService(ICreateResponse createResponse, IExceptionHandler exceptionHandler, ILogger<TransformDataService> logger)
    {
        _createResponse = createResponse;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
    }

    [Function("TransformDataService")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        CohortDistributionParticipant participant = null;

        TransformDataRequestBody requestBody;
        try
        {
            string requestBodyJson;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8))
            {
                requestBodyJson = reader.ReadToEnd();
            }

            requestBody = JsonSerializer.Deserialize<TransformDataRequestBody>(requestBodyJson);
        }
        catch
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        try
        {
            participant = requestBody.Participant;

            // This function is currently not using the screeningService, but it will do in the future
            // var screeningService = requestBody.ScreeningService;

            string json = await File.ReadAllTextAsync("transformRules.json");
            var rules = JsonSerializer.Deserialize<Workflow[]>(json);

            var re = new RulesEngine.RulesEngine(rules);

            var ruleParameters = new[] {
                new RuleParameter("participant", participant),
            };

            var resultList = await re.ExecuteAllRulesAsync("TransformData", ruleParameters);

            var transformedParticipant = new Participant()
            {
                FirstName = GetTransformedData<string>(resultList, "FirstName", participant.FirstName),
                Surname = GetTransformedData<string>(resultList, "Surname", participant.Surname),
                NhsNumber = GetTransformedData<string>(resultList, "NhsNumber", participant.NhsNumber),
                NamePrefix = GetTransformedData<string>(resultList, "NamePrefix", participant.NamePrefix),
                Gender = (Gender)GetTransformedData<int>(resultList, "Gender", Convert.ToInt32(participant.Gender)),
                OtherGivenNames = GetTransformedData<string>(resultList, "OtherGivenNames", participant.OtherGivenNames),
                PreviousSurname = GetTransformedData<string>(resultList, "PreviousSurname", participant.PreviousSurname),
                AddressLine1 = GetTransformedData<string>(resultList, "AddressLine1", participant.AddressLine1),
                AddressLine2 = GetTransformedData<string>(resultList, "AddressLine2", participant.AddressLine2),
                AddressLine3 = GetTransformedData<string>(resultList, "AddressLine3", participant.AddressLine3),
                AddressLine4 = GetTransformedData<string>(resultList, "AddressLine4", participant.AddressLine4),
                AddressLine5 = GetTransformedData<string>(resultList, "AddressLine5", participant.AddressLine5),
                Postcode = GetTransformedData<string>(resultList, "Postcode", participant.Postcode),
                TelephoneNumber = GetTransformedData<string>(resultList, "TelephoneNumber", participant.TelephoneNumber),
                MobileNumber = GetTransformedData<string>(resultList, "MobileNumber", participant.MobileNumber),
                EmailAddress = GetTransformedData<string>(resultList, "EmailAddress", participant.EmailAddress),
                ParticipantId = participant.ParticipantId
            };


            transformedParticipant.NamePrefix = await TransformNamePrefixAsync(transformedParticipant.NamePrefix);
            var transformString = new TransformString();
            transformedParticipant.FirstName = await transformString.CheckParticipantCharactersAsync(transformedParticipant.FirstName);
            transformedParticipant.OtherGivenNames = await transformString.CheckParticipantCharactersAsync(transformedParticipant.OtherGivenNames);
            transformedParticipant.Surname = await transformString.CheckParticipantCharactersAsync(transformedParticipant.Surname);
            transformedParticipant.PreviousSurname = await transformString.CheckParticipantCharactersAsync(transformedParticipant.PreviousSurname);
            transformedParticipant.AddressLine1 = await transformString.CheckParticipantCharactersAsync(transformedParticipant.AddressLine1);
            transformedParticipant.AddressLine2 = await transformString.CheckParticipantCharactersAsync(transformedParticipant.AddressLine2);
            transformedParticipant.AddressLine3 = await transformString.CheckParticipantCharactersAsync(transformedParticipant.AddressLine3);
            transformedParticipant.AddressLine4 = await transformString.CheckParticipantCharactersAsync(transformedParticipant.AddressLine4);
            transformedParticipant.AddressLine5 = await transformString.CheckParticipantCharactersAsync(transformedParticipant.AddressLine5);
            transformedParticipant.Postcode = await transformString.CheckParticipantCharactersAsync(transformedParticipant.Postcode);
            transformedParticipant.TelephoneNumber = await transformString.CheckParticipantCharactersAsync(transformedParticipant.TelephoneNumber);
            transformedParticipant.MobileNumber = await transformString.CheckParticipantCharactersAsync(transformedParticipant.MobileNumber);

            var response = JsonSerializer.Serialize(transformedParticipant);
            return _createResponse.CreateHttpResponse(HttpStatusCode.OK, req, response);
        }
        catch (Exception ex)
        {
            await _exceptionHandler.CreateSystemExceptionLogFromNhsNumber(ex, participant.NhsNumber, "");
            _logger.LogWarning(ex, "exception occured while running transform data service");
            return _createResponse.CreateHttpResponse(HttpStatusCode.InternalServerError, req);
        }
    }

    private T GetTransformedData<T>(List<RuleResultTree> results, string field, T CurrentValue)
    {
        var result = results.Find(x => x.Rule.RuleName.Split('.')[1] == field);
        return result?.ActionResult?.Output == null ? CurrentValue : (T)result.ActionResult.Output;
    }

    public async Task<string> TransformNamePrefixAsync(string namePrefix)
    {

        // Set up rules engine
        string json = await File.ReadAllTextAsync("namePrefixRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var re = new RulesEngine.RulesEngine(rules);

        namePrefix = namePrefix.ToUpper();

        var ruleParameters = new[] {
            new RuleParameter("namePrefix", namePrefix),
        };

        // Execute rules
        var rulesList = await re.ExecuteAllRulesAsync("NamePrefix", ruleParameters);

        // Assign new name prefix
        namePrefix = (string)rulesList.Where(result => result.IsSuccess)
                                                    .Select(result => result.ActionResult.Output)
                                                    .FirstOrDefault()
                                                    ?? namePrefix;

        return namePrefix;
    }

}
