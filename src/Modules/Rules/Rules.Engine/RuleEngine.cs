using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Engine;

public sealed class RuleEngine : IRuleEngine
{
    private readonly Dictionary<string, IRuleSet> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly RuleEngineOptions _options;
    private readonly IEnumerable<IRuleProvider> _providers;

    public RuleEngine(IEnumerable<IRuleProvider> providers, RuleEngineOptions options)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public RuleExecutionResult Execute(string ruleSetName, IRuleContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleSetName);
        ArgumentNullException.ThrowIfNull(context);

        var ruleSet = GetRuleSet(ruleSetName);
        if (ruleSet is null)
        {
            if (_options.ThrowIfRuleSetNotFound)
            {
                throw new InvalidOperationException($"RuleSet '{ruleSetName}' not found.");
            }

            return new RuleExecutionResult
            {
                RuleSetName = ruleSetName,
                ExecutedRules = Array.Empty<string>(),
                Output = new Dictionary<string, object>()
            };
        }

        var output = new RuleOutput();
        var executed = new List<string>();

        foreach (var rule in ruleSet.Rules)
        {
            if (!rule.Match(context))
            {
                continue;
            }

            rule.Apply(context, output);
            executed.Add(rule.Name);

            if (_options.StopOnFirstMatch)
            {
                break;
            }
        }

        return new RuleExecutionResult
        {
            RuleSetName = ruleSetName,
            ExecutedRules = executed,
            Output = output.Items
        };
    }

    private IRuleSet? GetRuleSet(string name)
    {
        if (_cache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var all = _providers
            .SelectMany(provider => provider.GetRuleSets())
            .ToList();

        var found = all.FirstOrDefault(set =>
            string.Equals(set.Name, name, StringComparison.OrdinalIgnoreCase));

        if (found is not null)
        {
            _cache[name] = found;
        }

        return found;
    }
}
