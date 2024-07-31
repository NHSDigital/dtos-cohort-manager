namespace NHS.CohortManager.CohortDistribution;

using System.Reflection;
using System.Text.RegularExpressions;
using Model;
using System.Text;
using System.Text.Json;
using RulesEngine.Models;

public class TransformString {
    private RulesEngine.RulesEngine _re;

    public TransformString()
    {
        string json = File.ReadAllText("characterRules.json");
        var rules = JsonSerializer.Deserialize<Workflow[]>(json);
        _re = new RulesEngine.RulesEngine(rules);
    }

    public async Task<Participant> CheckParticipantCharactersAync(Participant participant)
    {
        // Iterate through string fields of participant
        var stringFields = participant.GetType().GetProperties().Where(p => p.PropertyType == typeof(string));
        string allowedCharacters = @"^[\w\d\s.,\-()/='+:?!""%&;<>*]+$";

        foreach (PropertyInfo field in stringFields)
        {
            bool anyInvalidChars;
            var stringField = (string) field.GetValue(participant);

            try
            {
                anyInvalidChars = ! Regex.IsMatch(stringField, allowedCharacters);
            }
            catch (ArgumentNullException)
            {
                // Skip if the field is null
                continue;
            }

            if (anyInvalidChars)
            {
                if (stringField.Contains(@"\E\") | stringField.Contains(@"\E\")) {
                    throw new ArgumentException();
                }
                var transformedField = await TransformCharactersAsync(stringField);

                // Check to see if there are any unhandled invalid chars
                if (!Regex.IsMatch(transformedField, allowedCharacters))
                {
                    // Will call the exception service in the future
                    throw new ArgumentException();
                }

                field.SetValue(participant, transformedField);
            }
        }
        return participant;
    }

    public async Task<string> TransformCharactersAsync(string invalidString)
    {
        StringBuilder stringBuilder = new(invalidString.Length);

        foreach (char character in invalidString)
        {
            var rulesList = await _re.ExecuteAllRulesAsync("71.CharacterRules", character);
            var transformedCharacter = (char?) rulesList.Where(result => result.IsSuccess)
                                            .Select(result => result.ActionResult.Output)
                                            .FirstOrDefault();

            stringBuilder.Append(transformedCharacter);
        }
        return stringBuilder.ToString();
    }
}
