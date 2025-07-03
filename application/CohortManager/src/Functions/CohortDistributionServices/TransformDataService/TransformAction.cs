namespace NHS.CohortManager.CohortDistributionService;

using System.Reflection;
using Model;
using RulesEngine.Actions;
using RulesEngine.Models;
using RulesEngine.ExpressionBuilders;
using Model.Enums;

/// <summary>
/// Custom action that is passed into the rules engine to evaluate transformations.
/// </summary>
/// <param name="context">
/// Context fields passed in from the rules engine, contains the field of the participant to do the transformation on,
/// The new value (or an expression which will be evaluated to get the value),
/// and whether the transformation is an expression (code), or a value.
/// </param>
/// <returns>The transformed participant</returns>
class TransformAction : ActionBase
{
    public override async ValueTask<object?> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        try
        {
            var transformFields = context.GetContext<List<TransformFields>>("transformFields");
            if (transformFields == null)
            {

                throw new ArgumentNullException(paramName: nameof(context), message: "Transform fields context value cannot be null");
            }
            var participant = GetParameter<CohortDistributionParticipant>("participant", ruleParameters);
            var databaseParticipant = GetParameter<CohortDistribution>("databaseParticipant", ruleParameters);
            var existingParticipant = GetParameter<CohortDistributionParticipant>("existingParticipant", ruleParameters);

            if (participant == null)
            {
                throw new ArgumentNullException(paramName: nameof(ruleParameters), message: "Participant parameter cannot be null");
            }

            foreach (var transformField in transformFields)
            {
                if (transformField?.field == null)
                {
                    continue;
                }

                var property = typeof(CohortDistributionParticipant).GetProperty(transformField.field);

                if (property == null)
                {
                    continue;
                }

                if (transformField.isExpression)
                {
                    EvaluateExpression(property!, transformField.value, participant, databaseParticipant, existingParticipant);
                }
                else
                {
                    dynamic value;

                    switch (property!.PropertyType.Name)
                    {
                        case "string":
                            value = transformField.value;
                            break;
                        case "int":
                            value = int.Parse(transformField.value);
                            break;
                        case "Nullable`1":
                            value = Enum.Parse<Gender>(transformField.value);
                            break;
                        default:
                            value = transformField.value;
                            break;
                    }
                    property.SetValue(participant, value);
                }
            }
            return participant;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static T? GetParameter<T>(string name, RuleParameter[] ruleParameters) where T : class
    {
        return ruleParameters.FirstOrDefault(r => r.Name == name)?.Value as T;
    }

    private static void EvaluateExpression(PropertyInfo property, string expression, CohortDistributionParticipant participant, CohortDistribution? databaseParticipant, CohortDistributionParticipant? existingParticipant)
    {
        if (property == null || string.IsNullOrEmpty(expression))
    {
        return;
    }

        var reParser = new RuleExpressionParser(new ReSettings());
        var ruleParameters = new List<RuleParameter>
        {
            new RuleParameter("participant", participant)
        };

        if (databaseParticipant != null)
        {
            ruleParameters.Add(new RuleParameter("databaseParticipant", databaseParticipant));
        }

        if (existingParticipant != null)
        {
            ruleParameters.Add(new RuleParameter("existingParticipant", existingParticipant));
        }
        var result = reParser.Evaluate<string>(expression, ruleParameters.ToArray());
        if (result != null)
        {
            property.SetValue(participant, result);
        }
    }
}
