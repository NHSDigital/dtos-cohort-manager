namespace NHS.CohortManager.CohortDistributionServices;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Model;

public class DistributeParticipantActivities
{
    // TODO: add lookup & static validation
    [Function(nameof(ValidateParticipant))]
    public async Task<ValidationExceptionLog> ValidateParticipant(string fileName,
                                                        CohortDistributionParticipant requestParticipant,
                                                        CohortDistributionParticipant existingParticipant)
    {
        var request = new LookupValidationRequestBody
        {
            NewParticipant = new Participant(requestParticipant),
            ExistingParticipant = new Participant(existingParticipant),
            FileName = fileName,
            RulesType = RulesType.CohortDistribution
        };

        var json = JsonSerializer.Serialize(request);

        _logger.LogInformation("Called cohort validation service");
        var response = await GetResponseAsync(json, _config.LookupValidationURL);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }
        var exceptionLog = JsonSerializer.Deserialize<ValidationExceptionLog>(response);

        if (exceptionLog.CreatedException)
        {
            var errorMessage = $"Participant {participantData.ParticipantId} triggered a validation rule, so will not be added to cohort distribution";
            await HandleExceptionAsync(errorMessage, participantData, participantRecord.FileName!);

            var participantManagement = await _participantManagementClient.GetSingle(participantData.ParticipantId);
            participantManagement.ExceptionFlag = 1;

            var exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
            if (!exceptionFlagUpdated)
            {
                throw new IOException("Failed to update exception flag");
            }

            if (!ignoreParticipantExceptions)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Calls the Transform Data Service and returns the transformed participant
    /// </summary>
    /// <returns>
    /// The transformed CohortDistributionParticipant, or null if there were any exceptions during execution.
    /// </returns>
    [Function(nameof(TransformParticipant))]
    public async Task<CohortDistributionParticipant?> TransformParticipant(string serviceProvider,
                                                                CohortDistributionParticipant participantData,
                                                                CohortDistributionParticipant existingParticipant)
    {
        var transformDataRequestBody = new TransformDataRequestBody()
        {
            Participant = participantData,
            ServiceProvider = serviceProvider,
            ExistingParticipant = existingParticipant.ToCohortDistribution()
        };

        var json = JsonSerializer.Serialize(transformDataRequestBody);

        _logger.LogInformation("Called transform data service");
        var response = await GetResponseAsync(json, _config.TransformDataServiceURL);
        if (!string.IsNullOrEmpty(response))
        {
            return JsonSerializer.Deserialize<CohortDistributionParticipant>(response);
        }
        return null;
    }

    [Function(nameof(GetCohortDistributionRecord))]
    public async Task<CohortDistributionParticipant> GetCohortDistributionRecord(string participantId)
    {
        long longParticipantId = long.Parse(participantId);

        var cohortDistRecords = await _cohortDistributionClient.GetByFilter(x => x.ParticipantId == longParticipantId);
        var latestParticipant = cohortDistRecords.OrderByDescending(x => x.CohortDistributionId).FirstOrDefault();

        if (latestParticipant != null)
        {
            return new CohortDistributionParticipant(latestParticipant);
        }
        else
        {
            var participantToReturn = new CohortDistributionParticipant();
            participantToReturn.NhsNumber = "0";

            return participantToReturn;
        }
    }

    [Function(nameof(UpdateExceptionFlag))]
    public async Task UpdateExceptionFlag()
    {
        var errorMessage = $"Participant {participantData.ParticipantId} triggered a validation rule, so will not be added to cohort distribution";
        await HandleExceptionAsync(errorMessage, participantData, participantRecord.FileName!);

        var participantManagement = await _participantManagementClient.GetSingle(participantData.ParticipantId);
        participantManagement.ExceptionFlag = 1;

        var exceptionFlagUpdated = await _participantManagementClient.Update(participantManagement);
        if (!exceptionFlagUpdated)
        {
            throw new IOException("Failed to update exception flag");
        }

        if (!ignoreParticipantExceptions)
        {
            return;
        }
    }

    [Function(nameof(AllocateServiceProvider))]
    public async Task<string?> AllocateServiceProvider(string nhsNumber, string screeningAcronym, string postCode, string errorRecord)
    {

        if (string.IsNullOrEmpty(postCode))
        {
            return string.Empty;
        }
        var allocationConfigRequestBody = new AllocationConfigRequestBody
        {
            NhsNumber = nhsNumber,
            Postcode = postCode,
            ScreeningAcronym = screeningAcronym,
            ErrorRecord = errorRecord
        };

        var json = JsonSerializer.Serialize(allocationConfigRequestBody);

        var response = await GetResponseAsync(json, _config.AllocateScreeningProviderURL);

        if (!string.IsNullOrEmpty(response))
        {
            return response;
        }
        // return null;

    }

    /// <summary>
    /// Calls retrieve participant data which constructs a CohortDistributionParticipant
    /// based on the data from the Participant Management and Demographic tables.
    /// </summary>
    /// <returns>
    /// CohortDistributionParticipant, or null if there were any exceptions during execution.
    /// </returns>
    [Function(nameof(RetrieveParticipantData))]
    public async Task<CohortDistributionParticipant> RetrieveParticipantData(CreateCohortDistributionRequestBody cohortDistributionRequestBody)
    {
        var retrieveParticipantRequestBody = new RetrieveParticipantRequestBody()
        {
            NhsNumber = cohortDistributionRequestBody.NhsNumber,
            ScreeningService = cohortDistributionRequestBody.ScreeningService
        };

        var requestBody = JsonSerializer.Serialize(retrieveParticipantRequestBody);
        var response = await GetResponseAsync(requestBody, _config.RetrieveParticipantDataURL);

        if (!string.IsNullOrEmpty(response))
        {
            return JsonSerializer.Deserialize<CohortDistributionParticipant>(response);
        }

        // return null;
    }
}