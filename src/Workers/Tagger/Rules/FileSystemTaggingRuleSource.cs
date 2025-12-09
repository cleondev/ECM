using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class FileSystemTaggingRuleSource : ITaggingRuleSource
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FileSystemTaggingRuleSource> _logger;
    private readonly IOptionsMonitor<TaggingRuleFilesOptions> _options;

    public FileSystemTaggingRuleSource(
        IHostEnvironment environment,
        ILogger<FileSystemTaggingRuleSource> logger,
        IOptionsMonitor<TaggingRuleFilesOptions> options)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyCollection<TaggingRuleOptions> GetRules()
    {
        var configuredFiles = _options.CurrentValue?.Paths;
        if (configuredFiles is null || configuredFiles.Count == 0)
        {
            return Array.Empty<TaggingRuleOptions>();
        }

        var rules = new List<TaggingRuleOptions>();

        foreach (var path in configuredFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var resolved = ResolvePath(path);
            if (resolved is null)
            {
                continue;
            }

            var loadedRules = LoadRules(resolved);
            if (loadedRules.Count == 0)
            {
                continue;
            }

            rules.AddRange(loadedRules);
        }

        return rules;
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

        _logger.LogWarning("Tagging rule file {Path} does not exist.", fullPath);
        return null;
    }

    private IReadOnlyCollection<TaggingRuleOptions> LoadRules(string filePath)
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(filePath) ?? _environment.ContentRootPath)
                .AddJsonFile(Path.GetFileName(filePath), optional: false, reloadOnChange: false)
                .Build();

            var fileOptions = configuration.Get<TaggingRulesOptions>();
            if (fileOptions?.Rules is null || fileOptions.Rules.Count == 0)
            {
                _logger.LogInformation("Tagging rule file {Path} does not contain any rules.", filePath);
                return Array.Empty<TaggingRuleOptions>();
            }

            return fileOptions.Rules.ToArray();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load tagging rules from {Path}.", filePath);
            return Array.Empty<TaggingRuleOptions>();
        }
    }
}
