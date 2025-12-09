namespace Ecm.Rules.Abstractions;

public interface IRuleContext
{
    T Get<T>(string key, T defaultValue = default!);
    bool Has(string key);
    IReadOnlyDictionary<string, object> Items { get; }
}
