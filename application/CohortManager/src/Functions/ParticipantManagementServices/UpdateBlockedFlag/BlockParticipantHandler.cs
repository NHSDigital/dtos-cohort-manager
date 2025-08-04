namespace NHS.CohortManager.ParticipantManagementService;

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Net.Http.Headers;
using Model;
using RulesEngine.Models;

public class BlockParticipantHandler : IBlockParticipantHandler
{
    private readonly ILogger<BlockParticipantHandler> _logger;
    private readonly IDataServiceClient<ParticipantManagement> _participantManagementDataService;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographicDataService;
    private readonly IHttpClientFunction _httpClient;
    private readonly UpdateBlockedFlagConfig _config;
    public BlockParticipantHandler(ILogger<BlockParticipantHandler> logger,
        IDataServiceClient<ParticipantManagement> participantManagementDataService,
        IDataServiceClient<ParticipantDemographic> participantDemographicDataService,
        IHttpClientFunction httpClient,
        IOptions<UpdateBlockedFlagConfig> config
    )
    {
        _logger = logger;
        _participantManagementDataService = participantManagementDataService;
        _participantDemographicDataService = participantDemographicDataService;
        _httpClient = httpClient;
        _config = config.Value;
    }

    public async Task<BlockParticipantResult> BlockParticipant(BlockParticipantDTO blockParticipantRequest)
    {
        if (!ValidationHelper.ValidateNHSNumber(blockParticipantRequest.NhsNumber.ToString()))
        {
            _logger.LogWarning("Participant had an invalid NHS Number and cannot be blocked");
            throw new InvalidDataException("Invalid NHS Number");
        }

        var participantDemographic = await _participantDemographicDataService.GetSingleByFilter(x => x.NhsNumber == blockParticipantRequest.NhsNumber);

        if (!ValidateRecordsMatch(participantDemographic, blockParticipantRequest))
        {
            _logger.LogWarning("Participant had an didn't pass three point check and cannot be blocked");
            return new BlockParticipantResult(false, "Participant Didn't pass three point check");
        }

        var participantManagementRecord = await _participantManagementDataService.GetSingleByFilter(x => x.NHSNumber == blockParticipantRequest.NhsNumber);

        if (participantManagementRecord.BlockedFlag == 1)
        {
            _logger.LogWarning("Participant already blocked and cannot be blocked");
            return new BlockParticipantResult(false, "Participant Already Blocked");
        }

        if (participantManagementRecord != null)
        {
            return await BlockExistingParticipant(participantManagementRecord);
        }

        return await BlockNewParticipant(blockParticipantRequest);

    }

    private async Task<BlockParticipantResult> BlockNewParticipant(BlockParticipantDTO blockParticipantRequest)
    {
        var pdsParticipant = await GetPDSParticipant(blockParticipantRequest.NhsNumber);

        if (pdsParticipant == null || !ValidateRecordsMatch(pdsParticipant, blockParticipantRequest))
        {
            return new BlockParticipantResult(false, "Participant details do not match a records in Cohort Manager or PDS");
        }

        var participantManagementRecord = new ParticipantManagement
        {
            NHSNumber = pdsParticipant.NhsNumber,
            BlockedFlag = 1,
            EligibilityFlag = 0,
        };

        var participantManagementAdded = await _participantManagementDataService.Add(participantManagementRecord);

        if (!participantManagementAdded)
        {
            return new BlockParticipantResult(false, "Unable to add participant to Cohort Manager to be blocked");
        }

        return new BlockParticipantResult(true, "Participant Has been blocked");


    }

    private async Task<BlockParticipantResult> BlockExistingParticipant(ParticipantManagement participant)
    {
        var blockFlagUpdated = await SetBlockedFlag(participant, true);

        if (!blockFlagUpdated)
        {
            return new BlockParticipantResult(false, "Failed to Update participant in Cohort Manager");
        }

        var unsubscribeFromNems = await UnsubscribeParticipantFromNEMS(participant.NHSNumber);

        if (!unsubscribeFromNems)
        {
            return new BlockParticipantResult(false, "Failed to unsubscribe Participant From NEMS");
        }

        return new BlockParticipantResult(true, "Participant Has been blocked");

    }

    private async Task<bool> UnsubscribeParticipantFromNEMS(long nhsNumber)
    {
        var nemsUnsubscribeResponse = await _httpClient.SendPost(_config.ManageNemsSubscriptionUnsubscribeURL, CreateNhsNumberQueryParams(nhsNumber));

        return nemsUnsubscribeResponse.IsSuccessStatusCode;
    }

    private async Task<bool> SetBlockedFlag(ParticipantManagement participant, bool blocked)
    {
        participant.BlockedFlag = blocked ? (short)1 : (short)0;
        return await _participantManagementDataService.Update(participant);
    }

    private async Task<ParticipantDemographic> GetPDSParticipant(long nhsNumber)
    {
        var pdsResponse = await _httpClient.SendGet(_config.RetrievePdsDemographicURL, CreateNhsNumberQueryParams(nhsNumber));
        var pdsDemographic = JsonSerializer.Deserialize<ParticipantDemographic>(pdsResponse);

        return pdsDemographic;
    }

    private static bool ValidateRecordsMatch(ParticipantDemographic participant, BlockParticipantDTO dto)
    {

        if (!DateOnly.TryParseExact(participant.DateOfBirth, "yyyyMMdd", out var parsedDob))
        {
            return false;
        }
        return string.Equals(participant.FamilyName, dto.FamilyName, StringComparison.InvariantCultureIgnoreCase)
            && participant.NhsNumber == dto.NhsNumber
            && parsedDob == dto.DateOfBirth;
    }

    private static Dictionary<string, string> CreateNhsNumberQueryParams(long nhsNumber) =>
        new Dictionary<string, string>
        {
            {"nhsNumber",nhsNumber.ToString()}
        };







}
