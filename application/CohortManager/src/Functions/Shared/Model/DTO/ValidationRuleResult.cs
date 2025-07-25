namespace Model;

using RulesEngine.Models;

/// <summary>
/// Result of the validations, excludes fields that are not
/// needed that are in the full rules engine output
/// </summary>
public class ValidationRuleResult
{
    public string RuleName { get; set; }
    public string? RuleDescription { get; set; }
    public string? ExceptionMessage { get; set; }

    public ValidationRuleResult() {}

    public ValidationRuleResult(RuleResultTree ruleResultTree)
    {
        RuleName = ruleResultTree.Rule.RuleName;
        RuleDescription = ruleResultTree.ActionResult?.Output?.ToString();
        ExceptionMessage = ruleResultTree.ExceptionMessage;
    }
}