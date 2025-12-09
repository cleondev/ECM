using System.Globalization;
using System.Text.RegularExpressions;
using Ecm.Rules.Abstractions;
using Ecm.Rules.Providers.Lambda;

namespace Ecm.Rules.Providers.Json;

public static class JsonRuleCompiler
{
    private static readonly Regex ComparisonPattern = new(
        "^(?<field>[A-Za-z0-9_.]+)\\s*(?<op>>=|<=|==|!=|>|<)\\s*(?<value>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IRule Compile(JsonRuleDefinition def)
    {
        ArgumentNullException.ThrowIfNull(def);

        object matchFunc;
        if (string.IsNullOrWhiteSpace(def.Condition))
        {
            matchFunc = _ => true;
        }
        else
        {
            matchFunc = ctx => EvaluateCondition(def.Condition, ctx);
        }

        var applyFunc = (IRuleContext _, IRuleOutput output) =>
        {
            foreach (var kv in def.Set)
            {
                output.Set(kv.Key, kv.Value);
            }
        };

        var name = string.IsNullOrWhiteSpace(def.Name) ? "JsonRule" : def.Name;
        return new LambdaRule(name, matchFunc, applyFunc);
    }

    private static bool EvaluateCondition(string condition, IRuleContext context)
    {
        var andParts = condition
            .Split("&&", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var anyAnd = true;

        foreach (var andPart in andParts)
        {
            var orParts = andPart.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var orResult = orParts.Length == 0 || orParts.Any(part => EvaluateExpression(part, context));
            anyAnd &= orResult;
        }

        return anyAnd;
    }

    private static bool EvaluateExpression(string expression, IRuleContext context)
    {
        var match = ComparisonPattern.Match(expression);
        if (!match.Success)
        {
            return false;
        }

        var field = match.Groups["field"].Value;
        if (string.IsNullOrWhiteSpace(field))
        {
            return false;
        }

        var op = match.Groups["op"].Value;
        var rawValue = match.Groups["value"].Value.Trim().Trim('\"');

        var actual = context.Has(field)
            ? context.Get<object>(field)
            : context.Get<object>(field, null!);

        if (actual is null)
        {
            return false;
        }

        if (TryCompareNumbers(actual, rawValue, out var numericResult))
        {
            return CompareNumeric(op, numericResult);
        }

        var actualText = actual.ToString() ?? string.Empty;
        return CompareStrings(op, actualText, rawValue);
    }

    private static bool TryCompareNumbers(object actual, string expectedText, out int comparison)
    {
        comparison = 0;

        if (!decimal.TryParse(expectedText, NumberStyles.Any, CultureInfo.InvariantCulture, out var expected))
        {
            return false;
        }

        if (actual is IConvertible convertible)
        {
            try
            {
                var actualNumber = convertible.ToDecimal(CultureInfo.InvariantCulture);
                comparison = actualNumber.CompareTo(expected);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        if (decimal.TryParse(actual.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedActual))
        {
            comparison = parsedActual.CompareTo(expected);
            return true;
        }

        return false;
    }

    private static bool CompareNumeric(string op, int comparison)
    {
        return op switch
        {
            ">" => comparison > 0,
            "<" => comparison < 0,
            ">=" => comparison >= 0,
            "<=" => comparison <= 0,
            "==" => comparison == 0,
            "!=" => comparison != 0,
            _ => false
        };
    }

    private static bool CompareStrings(string op, string actual, string expected)
    {
        var comparison = string.Compare(actual, expected, StringComparison.OrdinalIgnoreCase);

        return op switch
        {
            "==" => comparison == 0,
            "!=" => comparison != 0,
            ">" => comparison > 0,
            "<" => comparison < 0,
            ">=" => comparison >= 0,
            "<=" => comparison <= 0,
            _ => false
        };
    }
}
