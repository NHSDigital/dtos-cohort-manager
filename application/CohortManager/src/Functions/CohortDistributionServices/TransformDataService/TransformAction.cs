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
    public override async ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        try
        {
            var transformFields = context.GetContext<List<TransformFields>>("transformFields");
            var participant = (CohortDistributionParticipant)ruleParameters.Where(rule => rule.Name == "participant").Select(result => result.Value).FirstOrDefault();
            var databaseParticipant = (CohortDistribution)ruleParameters.Where(rule => rule.Name == "databaseParticipant").Select(result => result.Value).FirstOrDefault();

            foreach (var transformField in transformFields)
            {
                var property = typeof(CohortDistributionParticipant).GetProperty(transformField.field);

                if (transformField.isExpression)
                {
                    EvaluateExpression(property!, transformField.value, participant, databaseParticipant);
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

    private static void EvaluateExpression(PropertyInfo property, string expression, CohortDistributionParticipant participant, CohortDistribution databaseParticipant)
    {
        var reParser = new RuleExpressionParser(new ReSettings());
        var ruleParameters = new RuleParameter[] { new RuleParameter("participant", participant), new RuleParameter("databaseParticipant", databaseParticipant) };
        var result = reParser.Evaluate<string>(expression, ruleParameters);

        property.SetValue(participant, result);
    }
}
