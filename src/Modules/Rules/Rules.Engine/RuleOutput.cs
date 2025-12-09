using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Engine;

public sealed class RuleOutput : IRuleOutput
{
    private readonly Dictionary<string, object> _items = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, object value) => _items[key] = value;

    public bool TryGet<T>(string key, out T value)
    {
        if (_items.TryGetValue(key, out var obj) && obj is T cast)
        {
            value = cast;
            return true;
        }

        value = default!;
        return false;
    }

    public IReadOnlyDictionary<string, object> Items => _items;
}
