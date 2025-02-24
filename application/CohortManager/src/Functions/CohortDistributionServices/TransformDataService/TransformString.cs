namespace NHS.CohortManager.CohortDistribution;

using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using RulesEngine.Models;
using Model;

public class TransformString
{
    private readonly RulesEngine.RulesEngine _ruleEngine;

    public TransformString()
    {
        string json = File.ReadAllText("characterRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var reSettings = new ReSettings {UseFastExpressionCompiler = false};
        _ruleEngine = new RulesEngine.RulesEngine(rules, reSettings);
    }

    public async Task<CohortDistributionParticipant> TransformStringFields(CohortDistributionParticipant participant)
    {
        participant.NamePrefix = await CheckParticipantCharactersAsync(participant.NamePrefix);
        participant.FirstName = await CheckParticipantCharactersAsync(participant.FirstName);
        participant.OtherGivenNames = await CheckParticipantCharactersAsync(participant.OtherGivenNames);
        participant.FamilyName = await CheckParticipantCharactersAsync(participant.FamilyName);
        participant.PreviousFamilyName = await CheckParticipantCharactersAsync(participant.PreviousFamilyName);
        participant.AddressLine1 = await CheckParticipantCharactersAsync(participant.AddressLine1);
        participant.AddressLine2 = await CheckParticipantCharactersAsync(participant.AddressLine2);
        participant.AddressLine3 = await CheckParticipantCharactersAsync(participant.AddressLine3);
        participant.AddressLine4 = await CheckParticipantCharactersAsync(participant.AddressLine4);
        participant.AddressLine5 = await CheckParticipantCharactersAsync(participant.AddressLine5);
        participant.Postcode = await CheckParticipantCharactersAsync(participant.Postcode);
        participant.TelephoneNumber = await CheckParticipantCharactersAsync(participant.TelephoneNumber);
        participant.MobileNumber = await CheckParticipantCharactersAsync(participant.MobileNumber);

        return participant;
    }

    public async Task<string> CheckParticipantCharactersAsync(string stringField)
    {
        string allowedCharacters = @"^[a-zA-Z0-9\d\s.,\-()\/='+:?!""%&;<>*]+$";
        TimeSpan matchTimeout = TimeSpan.FromSeconds(2); // Adjust timeout as needed

        // Skip if the field is null or doesn't have any invalid chars
        if (string.IsNullOrWhiteSpace(stringField) || Regex.IsMatch(stringField, allowedCharacters, RegexOptions.None, matchTimeout))
        {
            return stringField;
        }
        else
        {
            // Special characters that need to be handled separately
            if (stringField.Contains(@"\E\") || stringField.Contains(@"\T\"))
            {
                throw new ArgumentException("Participant contains illegal characters");
            }

            var transformedField = await TransformCharactersAsync(stringField);

            // Check to see if there are any unhandled invalid chars
            if (!Regex.IsMatch(transformedField, allowedCharacters, RegexOptions.None, matchTimeout))
            {
                throw new ArgumentException("Participant contains illegal characters");
            }
            return transformedField;
        }
    }

    private async Task<string> TransformCharactersAsync(string invalidString)
    {
        StringBuilder stringBuilder = new(invalidString.Length);

        foreach (char character in invalidString)
        {
            var asciiCode = (int)character;
            var rulesList = await _ruleEngine.ExecuteAllRulesAsync("71.CharacterRules", asciiCode);
            var transformedCharacter = (char?)rulesList.Where(result => result.IsSuccess)
                                            .Select(result => result.ActionResult.Output)
                                            .FirstOrDefault() ?? character;

            stringBuilder.Append(transformedCharacter);
        }
        return stringBuilder.ToString();
    }
}
