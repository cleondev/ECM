namespace Ecm.Rules.Abstractions;

public interface IRuleOutput
{
    void Set(string key, object value);
    bool TryGet<T>(string key, out T value);
    IReadOnlyDictionary<string, object> Items { get; }
}
