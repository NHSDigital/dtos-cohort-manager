namespace NHS.CohortManager.CohortDistribution;

using Data.Database;
using System.Reflection;
using RulesEngine.Actions;
using RulesEngine.Models;
using RulesEngine.ExpressionBuilders;
using Model;

class LookupAction : ActionBase
{
    public override async ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        var expression = context.GetContext<string>("Expression");
        var field = context.GetContext<string>("participantField");
        PropertyInfo property = typeof(CohortDistributionParticipant).GetProperty(field);

        var reParser = new RuleExpressionParser(new ReSettings());
        var result = reParser.Evaluate<string>(expression, ruleParameters);

        property.SetValue(participant, result);
        return participant;
    }
}