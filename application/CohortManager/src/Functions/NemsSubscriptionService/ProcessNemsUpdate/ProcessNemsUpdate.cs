namespace NHS.Screening.ProcessNemsUpdate;

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Hl7.Fhir.ElementModel.Types;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;

public class ProcessNemsUpdate
{
    private readonly ILogger<ProcessNemsUpdate> _logger;
    private readonly IFhirPatientDemographicMapper _fhirPatientDemographicMapper;
    private readonly ICreateBasicParticipantData _createBasicParticipantData;
    private readonly IAddBatchToQueue _addBatchToQueue;
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ProcessNemsUpdateConfig _config;

    public ProcessNemsUpdate(
        ILogger<ProcessNemsUpdate> logger,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue,
        IHttpClientFunction httpClientFunction,
        IExceptionHandler exceptionHandler,
        IOptions<ProcessNemsUpdateConfig> processNemsUpdateConfig)
    {
        _logger = logger;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _createBasicParticipantData = createBasicParticipantData;
        _addBatchToQueue = addBatchToQueue;
        _httpClientFunction = httpClientFunction;
        _exceptionHandler = exceptionHandler;
        _config = processNemsUpdateConfig.Value;
    }

    /// <summary>
    /// Function that processes files from the nems-messages blob container. There are a number of stages to this function:
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
    public async Task Run([BlobTrigger("nems-messages/{name}", Connection = "nemsmeshfolder_STORAGE")] Stream blobStream, string name)
    {
        try
        {
            var nhsNumber = await GetNhsNumberFromFile(blobStream, name);

            if (nhsNumber == null)
            {
                _logger.LogInformation("There is no NHS number, unable to continue.");
                return;
            }

            var pdsResponse = await RetrievePdsRecord(nhsNumber);
            if (pdsResponse!.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await processPdsResponse(pdsResponse, nhsNumber);
                // we can stop processing here as we know that not found means the participant ether needed an update or they were actually not found
                return;
            }

            if (pdsResponse == null)
            {
                _logger.LogInformation("There is no PDS record, unable to continue.");
                return;
            }

            var retrievedPdsRecord = await pdsResponse.Content.ReadFromJsonAsync<PdsDemographic>();

            if (retrievedPdsRecord?.NhsNumber == nhsNumber)
            {
                _logger.LogInformation("NHS numbers match, processing the retrieved PDS record.");
                await ProcessRecord(new Participant(retrievedPdsRecord));
            }
            else
            {
                await unsubscribeFromNems(nhsNumber, retrievedPdsRecord!);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error processing NEMS update.");
        }

    }

    private async Task processPdsResponse(HttpResponseMessage pdsResponse, string nhsNumber)
    {
        var errorResponse = await pdsResponse!.Content.ReadFromJsonAsync<PDSErrorResponse>();
        // we now create a record as an update record and send to the manage participant function. Reason for removal for date should be today and the reason for remove of ORR
        if (errorResponse!.issue.FirstOrDefault()!.details.coding.FirstOrDefault()!.code == "INVALIDATED_RESOURCE")
        {
            var pdsDemographic = new PdsDemographic()
            {
                NhsNumber = nhsNumber,
                PrimaryCareProvider = null,
                ReasonForRemoval = "ORR",
                RemovalEffectiveFromDate = System.DateTime.UtcNow.Date.ToString("yyyyMMdd")
            };
            var participant = new Participant(pdsDemographic);

            participant.EligibilityFlag = "0";
            //sends record for an update
            await ProcessRecord(participant);
        }
        _logger.LogError("the PDS function has returned a 404 error. function now stopping processing");

    }

    private async Task unsubscribeFromNems(string nhsNumber, PdsDemographic retrievedPdsRecord)
    {
        var supersededRecord = new PdsDemographic()
        {
            NhsNumber = nhsNumber,
            SupersededByNhsNumber = retrievedPdsRecord?.NhsNumber,
            PrimaryCareProvider = null,
            ReasonForRemoval = "ORR",
            RemovalEffectiveFromDate = System.DateTime.UtcNow.Date.ToString("yyyyMMdd")
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

            return _fhirPatientDemographicMapper.ParseFhirJsonNhsNumber(blobJson);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error getting the NHS number from the file.");
            return null;
        }
    }

    private async Task<HttpResponseMessage?> RetrievePdsRecord(string nhsNumber)
    {
        try
        {
            var queryParams = new Dictionary<string, string>()
            {
                {"nhsNumber", nhsNumber }
            };

            return await _httpClientFunction.GetPDSRecord(_config.RetrievePdsDemographicURL, queryParams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error retrieving the PDS record.");
            return null;
        }
    }

    private async Task ProcessRecord(Participant participant)
    {
        var updateRecord = new ConcurrentQueue<BasicParticipantCsvRecord>();


        // TODO validate NHS number in record before enqueuing
        // TODO validate all dates in record before enqueuing

        var basicParticipantCsvRecord = new BasicParticipantCsvRecord
        {
            BasicParticipantData = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = "NemsMessages",
            Participant = participant
        };

        updateRecord.Enqueue(basicParticipantCsvRecord);

        _logger.LogInformation("Sending record to the update queue.");
        await _addBatchToQueue.ProcessBatch(updateRecord, _config.UpdateQueueName);
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
