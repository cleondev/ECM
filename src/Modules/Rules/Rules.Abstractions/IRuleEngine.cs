namespace Ecm.Rules.Abstractions;

public interface IRuleEngine
{
    RuleExecutionResult Execute(string ruleSetName, IRuleContext context);
}

public sealed class RuleExecutionResult
{
    public string RuleSetName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> ExecutedRules { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Output { get; init; } = new Dictionary<string, object>();
}
