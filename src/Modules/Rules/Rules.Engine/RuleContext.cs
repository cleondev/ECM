using System.Globalization;
using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Engine;

public sealed class RuleContext : IRuleContext
{
    private readonly IReadOnlyDictionary<string, object> _items;

    public RuleContext(IReadOnlyDictionary<string, object> items)
    {
        _items = items;
    }

    public IReadOnlyDictionary<string, object> Items => _items;

    public T Get<T>(string key, T defaultValue = default!)
    {
        if (!_items.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        if (value is T cast)
        {
            return cast;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
        catch (InvalidCastException)
        {
            return defaultValue;
        }
        catch (FormatException)
        {
            return defaultValue;
        }
        catch (OverflowException)
        {
            return defaultValue;
        }
    }

    public bool Has(string key) => _items.ContainsKey(key);
}
