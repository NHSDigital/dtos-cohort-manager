namespace NHS.Screening.ReceiveCaasFile;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Model;
using Common;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;


    public class ReceiveCaasFile
    {
        private readonly ILogger<ReceiveCaasFile> _logger;
        private readonly ICallFunction _callFunction;

        public ReceiveCaasFile(ILogger<ReceiveCaasFile> logger,
                                ICallFunction callFunction)
        {
            _logger = logger;
            _callFunction = callFunction;
        }

        [Function(nameof(ReceiveCaasFile))]
        public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream stream, string name)
        {
            var badRecords = new Dictionary<int, string>();
            var cohort = new List<Participant>();
            var rowNumber = 0;

            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                Delimiter = ",",
                MissingFieldFound = null,
                HeaderValidated = null
            };

            try
            {
                using var blobStreamReader = new StreamReader(stream);
                using var csv = new CsvReader(blobStreamReader, config);

                csv.Context.RegisterClassMap<ParticipantMap>();

                foreach (var participant in csv.GetRecords<Participant>())
                {
                    rowNumber++;
                    try
                    {
                        if (participant != null)
                        {
                            cohort.Add(participant);
                        }
                    }
                    catch (Exception ex)
                    {
                        badRecords.Add(rowNumber, csv.Context.Parser.RawRecord);
                        _logger.LogError("Unable to create object on line {RowNumber}.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", rowNumber, ex.Message, ex.StackTrace);
                    }
                }
            }
            catch (HeaderValidationException ex)
            {
                _logger.LogError("Header validation failed.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to read csv.\nMessage:{ExMessage}.\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
            }

            try
            {
                if (cohort.Count > 0)
                {
                    var json = JsonSerializer.Serialize(cohort);
                    await _callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
                    _logger.LogInformation("Created {CohortCount} Objects.", cohort.Count);
                }
                else
                {
                    _logger.LogError("Failed to create {BadRecordCount} Objects.", badRecords.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to call function.\nMessage:{ExMessage}\nStack Trace: {ExStackTrace}", ex.Message, ex.StackTrace);
            }
        }
    }
