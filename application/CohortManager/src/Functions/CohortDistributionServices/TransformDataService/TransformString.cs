namespace NHS.CohortManager.CohortDistributionService;

using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using RulesEngine.Models;
using Model;
using Common;

public class TransformString
{
    private readonly RulesEngine.RulesEngine _ruleEngine;
    private readonly IExceptionHandler _exceptionHandler;
    private bool ParticipantUpdated;
    public TransformString(IExceptionHandler exceptionHandler)
    {
        string json = File.ReadAllText("characterRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        var reSettings = new ReSettings { UseFastExpressionCompiler = false };
        _ruleEngine = new RulesEngine.RulesEngine(rules, reSettings);
        _exceptionHandler = exceptionHandler;

        ParticipantUpdated = false;

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
        participant.EmailAddress = await CheckEmailCharacters(participant.EmailAddress);
        participant.Postcode = await CheckParticipantCharactersAsync(participant.Postcode);
        participant.TelephoneNumber = await CheckParticipantCharactersAsync(participant.TelephoneNumber);
        participant.MobileNumber = await CheckParticipantCharactersAsync(participant.MobileNumber);

        if (ParticipantUpdated)
        {
            await _exceptionHandler.CreateTransformExecutedExceptions(participant, "CharacterRules", 71);
        }

        return participant;
    }

    private async Task<string> CheckParticipantCharactersAsync(string stringField)
    {
        string allowedCharacters = @"^[a-zA-Z0-9\s.,\-()'+:?!¬￢*]+$";
        TimeSpan matchTimeout = TimeSpan.FromSeconds(2); // Adjust timeout as needed

        // Skip if the field is null or doesn't have any invalid chars
        if (string.IsNullOrWhiteSpace(stringField) || Regex.IsMatch(stringField, allowedCharacters, RegexOptions.None, matchTimeout))
        {
            return stringField;
        }
        else
        {
            ParticipantUpdated = true;
            // Special characters that need to be handled separately
            if (stringField.Contains(@"\E\") || stringField.Contains(@"\T\"))
                throw new ArgumentException($"Participant contains illegal characters");

            // The & character is the only illegal character that is transformed to a string instead of a char
            if (stringField.Contains('&'))
            {
                stringField = stringField.Replace("&", " and ");
            }

            var transformedField = await TransformCharactersAsync(stringField);

            // Check to see if there are any unhandled invalid chars
            if (!Regex.IsMatch(transformedField, allowedCharacters, RegexOptions.None, matchTimeout))

                throw new ArgumentException("Participant contains illegal characters");

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

    private async Task<string?> CheckEmailCharacters(string? emailAddress)
    {
        string? transformedEmail = emailAddress;
        var rulesList = await _ruleEngine.ExecuteAllRulesAsync("71.InvalidEmailCharacter", emailAddress);

        // Only modify transformedEmail if a rule was successful
        var successfulResult = rulesList.FirstOrDefault(result => result.IsSuccess);
        if (successfulResult != null)
        {
            transformedEmail = (string?)successfulResult.ActionResult.Output;
        }

        if (transformedEmail != emailAddress)
        {
            ParticipantUpdated = true;
        }

        return transformedEmail;
    }


}
