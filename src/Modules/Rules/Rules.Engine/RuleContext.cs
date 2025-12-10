using System.ComponentModel;
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

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        try
        {
            if (value is string s)
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    var converted = converter.ConvertFromInvariantString(s);
                    return converted is null ? defaultValue : (T)converted;
                }
            }

            var convertedValue = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            return (T)convertedValue;
        }
        catch (Exception e) when (
            e is InvalidCastException ||
            e is FormatException ||
            e is OverflowException)
        {
            return defaultValue;
        }
    }

    public bool Has(string key) => _items.ContainsKey(key);
}
