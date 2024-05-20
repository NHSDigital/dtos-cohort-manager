using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using Model;
using Common;

namespace NHS.Screening.ReceiveCaasFile
{
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

        [Function(nameof(receiveCaasFile))]
        public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream stream, string name)
        {
            //read incoming file
            using var blobStreamReader = new StreamReader(stream);

            string content = await blobStreamReader.ReadToEndAsync();

            // split content into lines
            var lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(1);

            //instantiate temporary object and failure count for summary
            Cohort cohort = new Cohort();
            int failures = 0;

            // Iterate through lines creating objects
            foreach (var item in lines)
            {
                try
                {
                    // split line into fields
                    Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                    var values = CSVParser.Split(item);

                    // populate object with mapped fields
                    var model = new Participant
                    {
                        NHSId = values[0],
                        SupersededByNhsNumber = values[1],
                        PrimaryCareProvider = values[2],
                        GpConnect = values[3],
                        NamePrefix = values[4],
                        FirstName = values[5],
                        OtherGivenNames = values[6],
                        Surname = values[7],
                        DateOfBirth = values[8],
                        Gender = values[9],
                        AddressLine1 = values[10],
                        AddressLine2 = values[11],
                        AddressLine3 = values[12],
                        AddressLine4 = values[13],
                        AddressLine5 = values[14],
                        Postcode = values[15],
                        ReasonForRemoval = values[16],
                        ReasonForRemovalEffectiveFromDate = values[17],
                        DateOfDeath = values[18],
                        TelephoneNumber = values[19],
                        MobileNumber = values[20],
                        EmailAddress = values[21],
                        PreferredLanguage = values[22],
                        IsInterpreterRequired = values[23],
                        Action = values[24].Trim(),
                    };

                    // Add object to list
                    cohort.cohort.Add(model);
                }
                catch (Exception ex)
                {
                    failures++;
                    _logger.LogInformation($"Unable to create object on line {cohort.cohort.Count}.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                }
            }

            // Send loaded objects to endpoint
            try
            {
                var json = JsonSerializer.Serialize(cohort);
                _callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Unable to call function.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
            }

            // file summary info to log
            _logger.LogInformation($"Created {cohort.cohort.Count} Objects.");
            _logger.LogInformation($"Failed to create {failures} Objects.");
        }
    }

}
