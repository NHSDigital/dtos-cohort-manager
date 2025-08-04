namespace NHS.Screening.ProcessNemsUpdate;

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using Common;
using Common.Interfaces;
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
            string? nhsNumber = await GetNhsNumberFromFile(blobStream, name);

            if (nhsNumber == null)
            {
                _logger.LogInformation("There is no NHS number, unable to continue.");
                return;
            }

            string? pdsRecord = await RetrievePdsRecord(nhsNumber);

            if (pdsRecord == null)
            {
                _logger.LogInformation("There is no PDS record, unable to continue.");
                return;
            }

            var retrievedPdsRecord = JsonSerializer.Deserialize<PdsDemographic>(pdsRecord);

            if (retrievedPdsRecord?.NhsNumber == nhsNumber)
            {
                _logger.LogInformation("NHS numbers match, processing the retrieved PDS record.");
                await ProcessRecord(retrievedPdsRecord);
            }

            else
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
                await ProcessRecord(supersededRecord);

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

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error processing NEMS update.");
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

    private async Task<string?> RetrievePdsRecord(string nhsNumber)
    {
        try
        {
            var queryParams = new Dictionary<string, string>()
            {
                {"nhsNumber", nhsNumber }
            };

            return await _httpClientFunction.SendGet(_config.RetrievePdsDemographicURL, queryParams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error retrieving the PDS record.");
            return null;
        }
    }

    private async Task ProcessRecord(PdsDemographic pdsDemographic)
    {
        var updateRecord = new ConcurrentQueue<BasicParticipantCsvRecord>();

        var participant = new Participant(pdsDemographic);

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
