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
    private readonly ProcessNemsUpdateConfig _config;

    public ProcessNemsUpdate(
        ILogger<ProcessNemsUpdate> logger,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        ICreateBasicParticipantData createBasicParticipantData,
        IAddBatchToQueue addBatchToQueue,
        IHttpClientFunction httpClientFunction,
        IOptions<ProcessNemsUpdateConfig> processNemsUpdateConfig)
    {
        _logger = logger;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _createBasicParticipantData = createBasicParticipantData;
        _addBatchToQueue = addBatchToQueue;
        _httpClientFunction = httpClientFunction;
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
    /// It's unclear from the Jira ticket what should be returned, but currently this function returns nothing, only logging information.
    /// </returns>
    [Function(nameof(ProcessNemsUpdate))]
    public async Task Run([BlobTrigger("nems-messages/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string name)
    {
        try
        {
            string nhsNumber = await GetNhsNumberFromFile(blobStream, name);

            PdsDemographic? pdsRecord = await RetrievePdsRecord(nhsNumber);

            if (pdsRecord == null)
            {
                _logger.LogInformation("There is no PDS record, unable to continue.");
                return;
            }

            if (pdsRecord.NhsNumber == nhsNumber)
            {
                _logger.LogInformation("NHS numbers match, processing the retrieved PDS record.");
                await ProcessRecord(pdsRecord);
            }

            else
            {
                var supersededRecord = new PdsDemographic()
                {
                    NhsNumber = nhsNumber,
                    SupersededByNhsNumber = pdsRecord.NhsNumber,
                    PrimaryCareProvider = null,
                    ReasonForRemoval = "ORR",
                    RemovalEffectiveFromDate = DateTime.Today.ToString("yyyyMMdd")
                };

                _logger.LogInformation("NHS numbers do not match, processing the superseded record.");
                await ProcessRecord(supersededRecord);

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

    private async Task<string> GetNhsNumberFromFile(Stream blobStream, string name)
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
            throw;
        }
    }

    private async Task<PdsDemographic?> RetrievePdsRecord(string nhsNumber)
    {
        try
        {
            var queryParams = new Dictionary<string, string>()
            {
                {"nhsNumber", nhsNumber }
            };

            var pdsDemographicResponse = await _httpClientFunction.SendGetResponse(_config.RetrievePdsDemographicURL, queryParams);

            if (pdsDemographicResponse.IsSuccessStatusCode)
            {
                var responseBody = await _httpClientFunction.GetResponseText(pdsDemographicResponse);
                return JsonSerializer.Deserialize<PdsDemographic>(responseBody);
            }

            var errorMessage = $"The PDS response was not successful. StatusCode: {pdsDemographicResponse.StatusCode}. Unable to process record.";
            _logger.LogError(errorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error retrieving the PDS record.");
            throw;
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
            Participant = _createBasicParticipantData.BasicParticipantData(participant),
            FileName = "NemsMessages",
            participant = participant
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
            throw;
        }
    }
}
