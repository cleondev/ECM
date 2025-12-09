using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;

namespace Ecm.Rules.Providers.Json;

public sealed class JsonRuleProvider : IRuleProvider
{
    private readonly IReadOnlyCollection<IRuleSet> _ruleSets;

    public JsonRuleProvider(IEnumerable<JsonRuleSetDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var sets = new List<IRuleSet>();

        foreach (var def in definitions)
        {
            if (def is null || string.IsNullOrWhiteSpace(def.Name))
            {
                continue;
            }

            var rules = def.Rules
                .Select(JsonRuleCompiler.Compile)
                .ToArray();

            sets.Add(new RuleSet(def.Name, rules));
        }

        _ruleSets = sets;
    }

    public string Source => "Json";

    public IEnumerable<IRuleSet> GetRuleSets() => _ruleSets;
}
