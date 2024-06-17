using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using Model;
using Common;
using Model.Enums;

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

        [Function(nameof(ReceiveCaasFile))]
        public async Task Run([BlobTrigger("inbound/{name}", Connection = "caasfolder_STORAGE")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            string content = await blobStreamReader.ReadToEndAsync();
            var lines = content.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(1);
            Cohort cohort = new()
            {
                FileName = name
            };
            int failures = 0;

            foreach (var item in lines)
            {
                try
                {
                    Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                    var values = CSVParser.Split(item);

                    var model = new Participant
                    {
                        RecordType = values[0],
                        ChangeTimeStamp = values[1],
                        SerialChangeNumber = values[2],
                        NHSId = values[3],
                        SupersededByNhsNumber = values[4],
                        PrimaryCareProvider = values[5],
                        PrimaryCareProviderEffectiveFrom = values[6],
                        CurrentPosting = values[7],
                        CurrentPostingEffectiveFrom = values[8],
                        PreviousPosting = values[9],
                        PreviousPostingEffectiveFrom = values[10],
                        NamePrefix = values[11],
                        FirstName = values[12],
                        OtherGivenNames = values[13],
                        Surname = values[14],
                        PreviousSurname = values[15],
                        DateOfBirth = values[16],
                        Gender = (Gender)Enum.Parse(typeof(Gender), values[17]),
                        AddressLine1 = values[18],
                        AddressLine2 = values[19],
                        AddressLine3 = values[20],
                        AddressLine4 = values[21],
                        AddressLine5 =  values[22],
                        Postcode = values[23],
                        PafKey = values[24],
                        UsualAddressEffectiveFromDate = values[25],
                        ReasonForRemoval = values[26],
                        ReasonForRemovalEffectiveFromDate = values[27],
                        DateOfDeath = values[28],
                        DeathStatus = values[28] == "null" ? null : (Status)Enum.Parse(typeof(Status), values[29]),
                        TelephoneNumber = values[30],
                        TelephoneNumberEffectiveFromDate = values[31],
                        MobileNumber = values[32],
                        MobileNumberEffectiveFromDate = values[33],
                        EmailAddress = values[34],
                        EmailAddressEffectiveFromDate = values[35],
                        PreferredLanguage = values[36],
                        IsInterpreterRequired = values[37],
                        InvalidFlag = values[38],
                        RecordIdentifier = values[39],
                        ChangeReasonCode = values[40].Trim(),
                    };

                    cohort.Participants.Add(model);
                }
                catch (Exception ex)
                {
                    failures++;
                    _logger.LogInformation($"Unable to create object on line {cohort.Participants.Count}.\nMessage:{ex.Message}\nStack Trace: {ex.StackTrace}");
                }
            }

            try
            {
                var json = JsonSerializer.Serialize(cohort);
                await _callFunction.SendPost(Environment.GetEnvironmentVariable("targetFunction"), json);
            }
            catch (Exception ex)
            {
            _logger.LogInformation("Unable to call function.\nMessage:{Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Created {ParticipantCount} Objects.", cohort.Participants.Count);
            _logger.LogInformation("Failed to create {Failures} Objects.", failures);
        }
    }

}
