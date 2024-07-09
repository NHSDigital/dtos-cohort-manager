namespace Common;

using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Model;

public class HandleException : IHandleException
{

    private readonly ICallFunction _callFunction;
    private readonly ILogger<HandleException> _logger;
    public HandleException(ICallFunction callFunction, ILogger<HandleException> logger)
    {
        _callFunction = callFunction;
        _logger = logger;
    }
    public async Task<Participant> CheckStaticValidationRules(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);

        try
        {
            return await CallExceptionFunction(Environment.GetEnvironmentVariable("LookupValidationURL"), json, participantCsvRecord.Participant);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Static validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return participantCsvRecord.Participant;
        }
    }

    public async Task<Participant> CheckLookupValidationRules(Participant existingParticipant, Participant newParticipant, string fileName, string workFlow)
    {
        var json = JsonSerializer.Serialize(new
        {
            ExistingParticipant = existingParticipant,
            NewParticipant = newParticipant,
            Workflow = workFlow,
            FileName = fileName
        });

        try
        {
            return await CallExceptionFunction(Environment.GetEnvironmentVariable("LookupValidationURL"), json, existingParticipant);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return existingParticipant;
        }
    }

    public async Task<Participant> CheckTransformationValidationRules(ParticipantCsvRecord participantCsvRecord)
    {
        var json = JsonSerializer.Serialize(participantCsvRecord);
        try
        {
            return await CallExceptionFunction(Environment.GetEnvironmentVariable("CheckTransFormationURL"), json, participantCsvRecord.Participant);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return participantCsvRecord.Participant;
        }
    }

    public async Task<bool> CallExceptionFunction(Participant participant, string fileName)
    {
        participant.ExceptionRaised = "Y";
        var response = await _callFunction.SendPost(Environment.GetEnvironmentVariable("ExceptionFunctionURL"), GetValidationExceptionJson(participant, fileName));
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<bool> DemographicDataRetrievedSuccessfully(Demographic demographicData, Participant participant, string fileName)
    {
        if (demographicData == null)
        {
            participant.ExceptionRaised = "Y";
            await _callFunction.SendPost(Environment.GetEnvironmentVariable("ExceptionFunctionURL"), GetValidationExceptionJson(participant, fileName));
            return false;
        }

        // Get all properties of the object
        PropertyInfo[] properties = demographicData.GetType().GetProperties();

        if (properties.All(property => property.GetValue(demographicData) == null))
        {
            participant.ExceptionRaised = "N";
            await _callFunction.SendPost(Environment.GetEnvironmentVariable("ExceptionFunctionURL"), GetValidationExceptionJson(participant, fileName));
            return false;
        }
        return true;
    }

    private async Task<Participant> CallExceptionFunction(string URL, string json, Participant participant)
    {
        try
        {
            var response = await _callFunction.SendPost(URL, json);
            var validationExceptionJson = await ReadValidationException(response);

            if (!ShouldCarryOn(response))
            {
                participant.ExceptionRaised = "Y";
                await _callFunction.SendPost(Environment.GetEnvironmentVariable("ExceptionFunctionURL"), validationExceptionJson);
                return participant;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Lookup validation failed.\nMessage: {ex.Message}\nParticipant: {ex.StackTrace}");
            return participant;
        }
        participant.ExceptionRaised = "N";
        await _callFunction.SendPost(Environment.GetEnvironmentVariable("ExceptionFunctionURL"), json);
        return participant;
    }

    private async Task<string> ReadValidationException(HttpWebResponse httpResponseData)
    {
        using (Stream stream = httpResponseData.GetResponseStream())
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                var responseText = await reader.ReadToEndAsync();
                return responseText;
            }
        }
    }

    private bool ShouldCarryOn(HttpWebResponse response)
    {
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return true;
        }
        return false;
    }

    private string GetValidationExceptionJson(Participant participant, string fileName)
    {
        return JsonSerializer.Serialize<ValidationException>(new ValidationException()
        {
            FileName = fileName,
            NhsNumber = participant.NhsNumber,
            DateCreated = DateTime.Now,
            DateResolved = DateTime.Now,
            RuleId = null,
            RuleDescription = "No demograpgic Data",
            RuleContent = "there was no demograpghic data",
            Category = 1,
            Cohort = "1",
            Fatal = 1
        });
    }

}
