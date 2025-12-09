namespace Ecm.Rules.Engine;

public sealed class RuleEngineOptions
{
    public bool StopOnFirstMatch { get; set; }
    public bool ThrowIfRuleSetNotFound { get; set; } = true;
}
