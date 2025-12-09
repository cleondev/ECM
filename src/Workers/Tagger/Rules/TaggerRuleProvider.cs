using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ecm.Rules.Abstractions;
using Ecm.Rules.Providers.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class TaggerRuleProvider : IRuleProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IHostEnvironment _environment;
    private readonly ILogger<TaggerRuleProvider> _logger;
    private readonly IOptionsMonitor<TaggerRulesOptions> _options;

    public TaggerRuleProvider(
        IHostEnvironment environment,
        ILogger<TaggerRuleProvider> logger,
        IOptionsMonitor<TaggerRulesOptions> options)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Source => "Tagger.Configuration";

    public IEnumerable<IRuleSet> GetRuleSets()
    {
        var definitions = AggregateDefinitions();

        if (definitions.Count == 0)
        {
            return Array.Empty<IRuleSet>();
        }

        var provider = new JsonRuleProvider(definitions.Values);
        return provider.GetRuleSets();
    }

    private Dictionary<string, JsonRuleSetDefinition> AggregateDefinitions()
    {
        var merged = new Dictionary<string, JsonRuleSetDefinition>(StringComparer.OrdinalIgnoreCase);
        var current = _options.CurrentValue;

        AddDefinitions(current.RuleSets, merged);

        foreach (var path in current.Files ?? Array.Empty<string>())
        {
            var resolved = ResolvePath(path);
            if (resolved is null)
            {
                continue;
            }

            var fromFile = LoadDefinitions(resolved);
            AddDefinitions(fromFile, merged);
        }

        return merged;
    }

    private void AddDefinitions(IEnumerable<JsonRuleSetDefinition>? definitions, IDictionary<string, JsonRuleSetDefinition> target)
    {
        if (definitions is null)
        {
            return;
        }

        foreach (var definition in definitions)
        {
            if (definition is null || string.IsNullOrWhiteSpace(definition.Name))
            {
                continue;
            }

            if (!target.TryGetValue(definition.Name, out var existing))
            {
                target[definition.Name] = Clone(definition);
                continue;
            }

            if (definition.Rules is null || definition.Rules.Count == 0)
            {
                continue;
            }

            existing.Rules.AddRange(definition.Rules);
        }
    }

    private string? ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fullPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(_environment.ContentRootPath, path);

        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        _logger.LogWarning("Tagger rule file {Path} does not exist.", fullPath);
        return null;
    }

    private IReadOnlyCollection<JsonRuleSetDefinition> LoadDefinitions(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<JsonRuleSetDefinition>>(json, SerializerOptions)
                   ?? new List<JsonRuleSetDefinition>();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load tagging rules from {Path}.", filePath);
            return Array.Empty<JsonRuleSetDefinition>();
        }
    }

    private static JsonRuleSetDefinition Clone(JsonRuleSetDefinition source)
    {
        return new JsonRuleSetDefinition
        {
            Name = source.Name,
            Rules = source.Rules?.Select(rule => new JsonRuleDefinition
            {
                Name = rule.Name,
                Condition = rule.Condition,
                Set = rule.Set is null
                    ? new Dictionary<string, object>()
                    : new Dictionary<string, object>(rule.Set, StringComparer.OrdinalIgnoreCase)
            }).ToList() ?? new List<JsonRuleDefinition>()
        };
    }
}
