namespace NHS.Screening.ProcessNemsUpdate;

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using DataServices.Client;
using FluentValidation.Validators;

public class ProcessNemsUpdate
{
    private readonly ILogger<ProcessNemsUpdate> _logger;
    private readonly IFhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IDataServiceClient<ParticipantDemographic> _participantDemographic;
    private readonly ProcessNemsUpdateConfig _config;
    private long nhsNumberLong;

    public ProcessNemsUpdate(
        ILogger<ProcessNemsUpdate> logger,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue,
        IHttpClientFunction httpClientFunction,
        IExceptionHandler exceptionHandler,
        IDataServiceClient<ParticipantDemographic> participantDemographic,
        IOptions<ProcessNemsUpdateConfig> processNemsUpdateConfig)
    {
        _logger = logger;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _createBasicParticipantData = createBasicParticipantData;
        _addBatchToQueue = addBatchToQueue;
        _httpClientFunction = httpClientFunction;
        _exceptionHandler = exceptionHandler;
        _participantDemographic = participantDemographic;
        _config = processNemsUpdateConfig.Value;
    }

    /// <summary>
    /// Function that processes files from the nems-updates blob container. There are a number of stages to this function:
    /// 1) Parse the NHS number from the received file.
    /// 2) Use the parsed NHS number to retrieve the PDS record.
    /// 3) Compare the retrieved PDS record NHS number against the parsed NHS number.
    /// 4) If the NHS numbers match, add the PDS record onto the correct participant management queue.
    /// 5) If the NHS numbers do not match, build the required superseded record, then add this record onto the correct participant management queue.
    /// 6) Also if the NHS numbers do not match, unsubscribe the parsed NHS number from NEMS.
    /// </summary>
    /// <returns>
    /// This function returns nothing, only logs information/errors for successful or failing tasks.
    /// </returns>
    [Function(nameof(ProcessNemsUpdate))]
    public async Task Run([BlobTrigger("nems-updates/{name}", Connection = "nemsmeshfolder_STORAGE")] Stream blobStream, string name)
    {
        try
        {
            var nhsNumber = await GetNhsNumberFromFile(blobStream, name);

            if (!ValidationHelper.ValidateNHSNumber(nhsNumber!))
            {
                _logger.LogError("There was a problem parsing the NHS number from blob store in the ProcessNemsUpdate function");
                throw new InvalidDataException("Invalid NHS Number");
            }
            nhsNumberLong = long.Parse(nhsNumber!);

            var pdsResponse = await RetrievePdsRecord(nhsNumber!);
            if (pdsResponse!.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await ProcessPdsResponse(pdsResponse, nhsNumber!);
                // we can stop processing here as we know that not found means the participant ether needed an update or they were actually not found
                return;
            }

            pdsResponse.EnsureSuccessStatusCode();

            var retrievedPdsRecord = await pdsResponse.Content.ReadFromJsonAsync<PdsDemographic>();

            if (retrievedPdsRecord?.NhsNumber == nhsNumber)
            {
                _logger.LogInformation("NHS numbers match, processing the retrieved PDS record.");
                await ProcessRecord(new Participant(retrievedPdsRecord!));
            }
            else
            {
                await UnsubscribeFromNems(nhsNumber!, retrievedPdsRecord!);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error processing NEMS update.");
        }

    }

    private async Task ProcessPdsResponse(HttpResponseMessage pdsResponse, string nhsNumber)
    {
        var errorResponse = await pdsResponse!.Content.ReadFromJsonAsync<PdsErrorResponse>();
        // we now create a record as an update record and send to the manage participant function. Reason for removal for date should be today and the reason for remove of ORR
        if (errorResponse!.issue!.FirstOrDefault()!.details!.coding!.FirstOrDefault()!.code == "INVALIDATED_RESOURCE")
        {
            var pdsDemographic = new PdsDemographic()
            {
                NhsNumber = nhsNumber,
                PrimaryCareProvider = null,
                ReasonForRemoval = "ORR",
                RemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString("yyyyMMdd")
            };
            var participant = new Participant(pdsDemographic);
            participant.RecordType = Actions.Removed;
            //sends record for an update
            await ProcessRecord(participant);
            return;
        }
        _logger.LogError("the PDS function has returned a 404 error. function now stopping processing");

    }

    private async Task UnsubscribeFromNems(string nhsNumber, PdsDemographic retrievedPdsRecord)
    {
        var supersededRecord = new PdsDemographic()
        {
            NhsNumber = nhsNumber,
            SupersededByNhsNumber = retrievedPdsRecord?.NhsNumber,
            PrimaryCareProvider = null,
            ReasonForRemoval = "ORR",
            RemovalEffectiveFromDate = DateTime.UtcNow.Date.ToString("yyyyMMdd")
        };

        _logger.LogInformation("NHS numbers do not match, processing the superseded record.");
        await ProcessRecord(new Participant(supersededRecord));

        /*information exception raised for RuleId 60 and Rule name 'SupersededNhsNumber'*/
        var ruleId = 60;  // Rule 60 is for Superseded rule
        var ruleName = "SupersededNhsNumber"; //Superseded rule name
        await _exceptionHandler.CreateTransformExecutedExceptions(supersededRecord.ToCohortDistributionParticipant(), ruleName, ruleId);

        var unsubscribedFromNems = await UnsubscribeNems(nhsNumber);

        if (unsubscribedFromNems)
        {
            _logger.LogInformation("Successfully unsubscribed from NEMS.");
        }
    }

    private async Task<string?> GetNhsNumberFromFile(Stream blobStream, string name)
    {
        try
        {
            _logger.LogInformation("Downloading file from the blob, file: {Name}.", name);

            string blobJson;
            using (var reader = new StreamReader(blobStream, Encoding.UTF8))
            {
                blobJson = await reader.ReadToEndAsync();
            }

            // Determine format based on file extension and call appropriate parser
            if (name.EndsWith(".xml", System.StringComparison.OrdinalIgnoreCase))
            {
                return _fhirPatientDemographicMapper.ParseFhirXmlNhsNumber(blobJson);
            }
            else
            {
                return _fhirPatientDemographicMapper.ParseFhirJsonNhsNumber(blobJson);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error getting the NHS number from the file.");
            return null;
        }
    }

    private async Task<HttpResponseMessage> RetrievePdsRecord(string nhsNumber)
    {
        var queryParams = new Dictionary<string, string>()
        {
            {"nhsNumber", nhsNumber }
        };

        return await _httpClientFunction.GetPDSRecord(_config.RetrievePdsDemographicURL, queryParams);
    }

    private async Task ProcessRecord(Participant participant)
    {
        var updateRecord = new ConcurrentQueue<BasicParticipantCsvRecord>();

        // TODO validate all dates in record before enqueuing
        var existingParticipant = await _participantDemographic.GetSingleByFilter(x => x.NhsNumber == nhsNumberLong);


        if (existingParticipant == null)
        {
            participant.RecordType = Actions.New;
            _logger.LogWarning("The participant doesn't exists in Cohort Manager.A new record will be created in Cohort Manager.");
        }
        else
        {
            participant.RecordType = participant.RecordType != Actions.Removed ? Actions.Amended : participant.RecordType;
            _logger.LogWarning("The participant already exists in Cohort Manager. Existing record will get updated.");
        }

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            BasicParticipantData = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = "NemsMessages",
            Participant = participant
        };

        updateRecord.Enqueue(basicParticipantCsvRecord);

        _logger.LogInformation("Sending record to the update queue.");

        await _addBatchToQueue.ProcessBatch(updateRecord, _config.ParticipantManagementTopic);
    }

    private async Task<bool> UnsubscribeNems(string nhsNumber)
    {
        try
        {
            var data = new NameValueCollection { { "NhsNumber", nhsNumber } };
            var response = await _httpClientFunction.SendPost(_config.UnsubscribeNemsSubscriptionUrl, JsonSerializer.Serialize(data));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error unsubscribing from NEMS.");
            return false;
        }
    }
}
