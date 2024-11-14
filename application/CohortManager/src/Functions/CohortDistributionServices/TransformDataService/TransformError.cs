namespace NHS.CohortManager.CohortDistribution;

using RulesEngine.Actions;
using RulesEngine.Models;

/// <summary>
/// Custom action that is passed into the rules engine to raise an exception.
/// </summary>
/// <param name="context">
/// Context fields passed in from the rules engine, contains the exceptionMessage to be used.
/// </param>
/// <returns>A new Exception</returns>
class TransformError : ActionBase
{
    public override async ValueTask<object> Run(ActionContext context, RuleParameter[] ruleParameters)
    {
        var exceptionMessage = context.GetContext<string>("exceptionMessage");
        return new Exception(exceptionMessage);
    }

}
