namespace NHS.CohortManager.CohortDistribution;

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
        bool isExpression = context.GetContext<bool>("isExpression");
        var field = context.GetContext<string>("participantField");
        CohortDistributionParticipant participant = (CohortDistributionParticipant)ruleParameters[0].Value;
        PropertyInfo property = typeof(CohortDistributionParticipant).GetProperty(field);

        if (isExpression)
            {
                var expression = context.GetContext<string>("transformedValue");
                return EvaluateExpression(property, expression, participant);
            }

        dynamic value;

        switch (property.PropertyType.Name)
        {
            case "string":
                value = context.GetContext<string>("transformedValue");
                break;
            case "int":
                value = context.GetContext<int>("transformedValue");
                break;
            case "Nullable`1":
                value = context.GetContext<Gender>("transformedValue");
                break;
            default:
                value = context.GetContext<string>("transformedValue");
                break;
        }
        property.SetValue(participant, value);
        return participant;
    }

    public CohortDistributionParticipant EvaluateExpression(PropertyInfo property, string expresison,
                                                            CohortDistributionParticipant participant)
    {
        var reParser = new RuleExpressionParser(new ReSettings());
        var ruleParameters = new RuleParameter[] {new RuleParameter("participant", participant)};
        var result = reParser.Evaluate<string>(expresison, ruleParameters);

        property.SetValue(participant, result);

        return participant;
    }
}