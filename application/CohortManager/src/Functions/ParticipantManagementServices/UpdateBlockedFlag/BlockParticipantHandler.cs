namespace NHS.CohortManager.ParticipantManagementService;

using System;
using System.Globalization;
using System.Text.Json;
using Common;
using DataServices.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

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

    public async Task<BlockParticipantResult> BlockParticipant(BlockParticipantDto blockParticipantRequest)
    {
        if (!ValidationHelper.ValidateNHSNumber(blockParticipantRequest.NhsNumber.ToString()))
        {
            _logger.LogWarning("Participant had an invalid NHS Number and cannot be blocked");
            return new BlockParticipantResult(false, "Invalid NHS Number");
        }



        var participantManagementRecord = await _participantManagementDataService.GetSingleByFilter(x => x.NHSNumber == blockParticipantRequest.NhsNumber);

        if (participantManagementRecord == null)
        {
            return await BlockNewParticipant(blockParticipantRequest);
        }

        if (participantManagementRecord.BlockedFlag == 1)
        {
            _logger.LogWarning("Participant already blocked and cannot be blocked");
            return new BlockParticipantResult(false, "Participant Already Blocked");
        }

        var participantDemographic = await _participantDemographicDataService.GetSingleByFilter(x => x.NhsNumber == blockParticipantRequest.NhsNumber);

        if (!ValidateRecordsMatch(participantDemographic, blockParticipantRequest))
        {
            _logger.LogWarning("Participant didn't pass three point check and cannot be blocked");
            return new BlockParticipantResult(false, "Participant Didn't pass three point check");
        }

        _logger.LogInformation("Participant has been blocked");
        return await BlockExistingParticipant(participantManagementRecord);



    }

    public async Task<BlockParticipantResult> UnblockParticipant(long nhsNumber)
    {
        if (!ValidationHelper.ValidateNHSNumber(nhsNumber.ToString()))
        {
            _logger.LogWarning("Invalid NHS Number and cannot be unblocked");
            return new BlockParticipantResult(false, "Invalid NHS Number");
        }

        var participantManagementRecord = await _participantManagementDataService.GetSingleByFilter(x => x.NHSNumber == nhsNumber);

        if (participantManagementRecord.BlockedFlag != 1)
        {
            _logger.LogInformation("Participant couldn't be unblocked as they are not currently blocked");
            return new BlockParticipantResult(false, "Participant is not blocked");
        }


        var blockedFlagSet = await SetBlockedFlag(participantManagementRecord, false);
        if (!blockedFlagSet)
        {
            return new BlockParticipantResult(false, "Failed to unset blocked flag");
        }

        if (participantManagementRecord.EligibilityFlag == 1)
        {
            return new BlockParticipantResult(true, "Participant was unblocked but not resubscribed to Nems as they are ineligible");
        }

        var nemsSubscribed = await SubscribeParticipantToNEMS(nhsNumber);
        if (!nemsSubscribed)
        {
            return new BlockParticipantResult(false, "Participant couldn't be subscribed in Nems");
        }

        _logger.LogInformation("Participant has been unblocked");
        return new BlockParticipantResult(true, "Participant Unblocked");


    }

    public async Task<BlockParticipantResult> GetParticipant(BlockParticipantDto blockParticipantRequest)
    {

        if (!ValidationHelper.ValidateNHSNumber(blockParticipantRequest.NhsNumber.ToString()))
        {
            _logger.LogWarning("Participant had an invalid NHS Number and cannot be blocked");
            return new BlockParticipantResult(false, "Invalid NHS Number");
        }

        var participantDemographic = await _participantDemographicDataService.GetSingleByFilter(x => x.NhsNumber == blockParticipantRequest.NhsNumber);

        if (participantDemographic != null)
        {
            var recordsMatch = ValidateRecordsMatch(participantDemographic, blockParticipantRequest);
            var responseBody = JsonSerializer.Serialize(new BlockParticipantDto
            {
                NhsNumber = participantDemographic.NhsNumber,
                FamilyName = participantDemographic.FamilyName!,
                DateOfBirth = participantDemographic.DateOfBirth!,
            });
            return new BlockParticipantResult(recordsMatch, responseBody);
        }

        var pdsParticipant = await GetPDSParticipant(blockParticipantRequest.NhsNumber);

        if (pdsParticipant == null)
        {
            return new BlockParticipantResult(false, "Participant Couldn't be found");
        }

        var pdsRecordsMatch = ValidateRecordsMatch(pdsParticipant, blockParticipantRequest);
        var pdsResponseBody = JsonSerializer.Serialize(new BlockParticipantDto
        {
            NhsNumber = pdsParticipant.NhsNumber,
            FamilyName = pdsParticipant.FamilyName!,
            DateOfBirth = pdsParticipant.DateOfBirth!
        });

        return new BlockParticipantResult(pdsRecordsMatch, pdsResponseBody);

    }

    private async Task<BlockParticipantResult> BlockNewParticipant(BlockParticipantDto blockParticipantRequest)
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

    private async Task<bool> SubscribeParticipantToNEMS(long nhsNumber)
    {
        var nemsSubscribeResponse = await _httpClient.SendPost(_config.ManageNemsSubscriptionSubscribeURL, CreateNhsNumberQueryParams(nhsNumber));

        return nemsSubscribeResponse.IsSuccessStatusCode;
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

        return pdsDemographic!;
    }

    private static bool ValidateRecordsMatch(ParticipantDemographic participant, BlockParticipantDto dto)
    {

        if (!DateOnly.TryParseExact(dto.DateOfBirth, "yyyy-MM-dd",new CultureInfo("en-GB"),DateTimeStyles.None, out var dtoDateOfBirth ))
        {
            throw new FormatException("Date of Birth not in the correct format");
        }

        if (!DateOnly.TryParseExact(participant.DateOfBirth, "yyyyMMdd",new CultureInfo("en-GB"),DateTimeStyles.None, out var parsedDob))
        {
            return false;
        }
        return string.Equals(participant.FamilyName, dto.FamilyName, StringComparison.InvariantCultureIgnoreCase)
            && participant.NhsNumber == dto.NhsNumber
            && parsedDob == dtoDateOfBirth;
    }

    private static Dictionary<string, string> CreateNhsNumberQueryParams(long nhsNumber) =>
        new Dictionary<string, string>
        {
            {"nhsNumber",nhsNumber.ToString()}
        };

}
