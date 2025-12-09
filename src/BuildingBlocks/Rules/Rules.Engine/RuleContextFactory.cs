using System.Collections.Concurrent;
using System.Reflection;
using Ecm.Rules.Abstractions;

namespace Ecm.Rules.Engine;

public sealed class RuleContextFactory : IRuleContextFactory
{
    public IRuleContext FromDictionary(IDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var comparer = data is Dictionary<string, object> dictionary
            ? dictionary.Comparer
            : StringComparer.OrdinalIgnoreCase;

        var items = new Dictionary<string, object>(data, comparer);
        return new RuleContext(items);
    }

    public IRuleContext FromObject(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var properties = GetProperties(source.GetType());
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in properties)
        {
            if (!property.CanRead)
            {
                continue;
            }

            var value = property.GetValue(source);
            values[property.Name] = value ?? string.Empty;
        }

        return new RuleContext(values);
    }

    private static PropertyInfo[] GetProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, key =>
            key.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
}
