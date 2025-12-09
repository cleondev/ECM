namespace Ecm.Rules.Providers.Json;

public sealed class JsonRuleSetDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<JsonRuleDefinition> Rules { get; set; } = new();
}

public sealed class JsonRuleDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public Dictionary<string, object> Set { get; set; } = new();
}
