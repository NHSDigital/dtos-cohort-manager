namespace NHS.Screening.ProcessNemsUpdate;

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
    private readonly IHttpClientFunction _httpClientFunction;
    private readonly ProcessNemsUpdateConfig _config;

    public ProcessNemsUpdate(
        ILogger<ProcessNemsUpdate> logger,
        IFhirPatientDemographicMapper fhirPatientDemographicMapper,
        IHttpClientFunction httpClientFunction,
        IOptions<ProcessNemsUpdateConfig> processNemsUpdateConfig)
    {
        _logger = logger;
        _fhirPatientDemographicMapper = fhirPatientDemographicMapper;
        _httpClientFunction = httpClientFunction;
        _config = processNemsUpdateConfig.Value;
    }

    [Function(nameof(ProcessNemsUpdate))]
    public async Task Run([BlobTrigger("nems-messages/{name}", Connection = "caasfolder_STORAGE")] Stream blobStream, string name)
    {
        try
        {
            string nhsNumber = await GetNhsNumberFromFile(blobStream, name);

            PdsDemographic pdsRecord = await RetrievePdsRecord(nhsNumber);

            if (pdsRecord == null)
            {
                _logger.LogInformation("There is no PDS record, unable to continue.");
                return;
            }

            if (pdsRecord.NhsNumber == nhsNumber)
            {
                _logger.LogInformation("NHS numbers match.");
                _logger.LogInformation("Process the PDS record: {PdsRecord}", JsonSerializer.Serialize(pdsRecord));
                // need to check how the Update queue works in ProcessCaasFile, then implement the same here
            }

            else
            {
                _logger.LogInformation("NHS numbers do not match.");

                var supersededRecord = new PdsDemographic()
                {
                    NhsNumber = nhsNumber,
                    SupersededByNhsNumber = pdsRecord.NhsNumber,
                    PrimaryCareProvider = null,
                    ReasonForRemoval = "ORR",
                    RemovalEffectiveFromDate = DateTime.Today.ToString("yyyyMMdd"),
                    // EligibilityFlag = 0,
                    // RecordType = "AMENDED"
                };

                _logger.LogInformation("Process built superseded record: {Superseded}", JsonSerializer.Serialize(supersededRecord));
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

    private async Task<PdsDemographic> RetrievePdsRecord(string nhsNumber)
    {
        try
        {
            var queryParams = new Dictionary<string, string>()
            {
                // {"nhsNumber", "9111231130" } // NotFound
                // {"nhsNumber", "9000000025" } // Not matching
                {"nhsNumber", nhsNumber } // Matching
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
}
