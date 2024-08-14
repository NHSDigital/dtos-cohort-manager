namespace NHS.CohortManager.CohortDistribution;

using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using RulesEngine.Models;

public class TransformString
{
    private RulesEngine.RulesEngine _ruleEngine;

    public TransformString()
    {
        string json = File.ReadAllText("characterRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        _ruleEngine = new RulesEngine.RulesEngine(rules);
    }

    public async Task<string> CheckParticipantCharactersAsync(string stringField)
    {
        string allowedCharacters = @"^[\w\d\s.,\-()/='+:?!""%&;<>*]+$";

        // Skip if the field is null or doesn't have any invalid chars
        if (string.IsNullOrWhiteSpace(stringField) || Regex.IsMatch(stringField, allowedCharacters))
        {
            return stringField;
        }
        else
        {
            // Special characters that need to be handled separately
            if (stringField.Contains(@"\E\") || stringField.Contains(@"\T\"))
            {
                throw new ArgumentException();
            }

            var transformedField = await TransformCharactersAsync(stringField);

            // Check to see if there are any unhandled invalid chars
            if (!Regex.IsMatch(transformedField, allowedCharacters))
            {
                // Will call the exception service in the future
                throw new ArgumentException();
            }
            return transformedField;
        }
    }

    public async Task<string> TransformCharactersAsync(string invalidString)
    {
        StringBuilder stringBuilder = new(invalidString.Length);

        foreach (char character in invalidString)
        {
            var rulesList = await _ruleEngine.ExecuteAllRulesAsync("71.CharacterRules", character);
            var transformedCharacter = (char?)rulesList.Where(result => result.IsSuccess)
                                            .Select(result => result.ActionResult.Output)
                                            .FirstOrDefault();

            stringBuilder.Append(transformedCharacter);
        }
        return stringBuilder.ToString();
    }
}
