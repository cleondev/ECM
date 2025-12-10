using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;
using Ecm.Rules.Providers.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Tagger.Rules.Configuration;
using Tagger.RulesConfiguration;

using Xunit;

namespace Tagger.Tests;

public class TaggingRuleEngineTests
{
    [Fact]
    public void Evaluate_ReturnsTagWhenConditionsMatch()
    {
        var tagId = Guid.NewGuid();
        var options = new TaggerRulesOptions
        {
            RuleSets =
            {
                new JsonRuleSetDefinition
                {
                    Name = TaggingRuleSetNames.DocumentUploaded,
                    Rules =
                    {
                        new JsonRuleDefinition
                        {
                            Name = "PDF Uploads",
                            Condition = "extension == \".pdf\" && uploadedBy == \"alice\"",
                            Set = new Dictionary<string, object>
                            {
                                ["TagIds"] = new[] { tagId }
                            }
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options, Directory.GetCurrentDirectory());
        var context = CreateContext(new Dictionary<string, string>
        {
            ["extension"] = ".PDF",
            ["uploadedBy"] = "Alice"
        });

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);

        Assert.Contains(tagId, GetTagIds(result));
    }

    [Fact]
    public void Evaluate_SkipsRulesWhenRuleSetDoesNotMatch()
    {
        var options = new TaggerRulesOptions
        {
            RuleSets =
            {
                new JsonRuleSetDefinition
                {
                    Name = TaggingRuleSetNames.OcrCompleted,
                    Rules =
                    {
                        new JsonRuleDefinition
                        {
                            Name = "OCR Only",
                            Condition = "classification == \"invoice\"",
                            Set = new Dictionary<string, object>
                            {
                                ["TagIds"] = new[] { Guid.NewGuid() }
                            }
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options, Directory.GetCurrentDirectory());
        var context = CreateContext(new Dictionary<string, string>
        {
            ["classification"] = "invoice"
        });

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);

        Assert.Empty(GetTagIds(result));
    }

    [Fact]
    public void Evaluate_SupportsCompositeConditions()
    {
        var tagId = Guid.NewGuid();
        var options = new TaggerRulesOptions
        {
            RuleSets =
            {
                new JsonRuleSetDefinition
                {
                    Name = TaggingRuleSetNames.DocumentUploaded,
                    Rules =
                    {
                        new JsonRuleDefinition
                        {
                            Name = "High Value",
                            Condition = "ownerId == \"123\" || content == \"confidential\"",
                            Set = new Dictionary<string, object>
                            {
                                ["TagIds"] = new[] { tagId }
                            }
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options, Directory.GetCurrentDirectory());
        var context = CreateContext(new Dictionary<string, string>
        {
            ["content"] = "confidential"
        }, content: "confidential");

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);

        Assert.Contains(tagId, GetTagIds(result));
    }

    [Fact]
    public void Evaluate_MergesRulesFromInlineAndFile()
    {
        var inlineTag = Guid.NewGuid();
        var fileTag = Guid.NewGuid();
        var tempDirectory = Directory.CreateTempSubdirectory();
        var ruleFile = Path.Combine(tempDirectory.FullName, "rules.json");

        File.WriteAllText(
            ruleFile,
            $$"""
            [
              {
                "name": "{TaggingRuleSetNames.OcrCompleted}",
                "rules": [
                  {
                    "name": "From File",
                    "condition": "title == \"Report\"",
                    "set": { "TagIds": ["{fileTag}"] }
                  }
                ]
              }
            ]
            """
        );

        var options = new TaggerRulesOptions
        {
            Files = { ruleFile },
            RuleSets =
            {
                new JsonRuleSetDefinition
                {
                    Name = TaggingRuleSetNames.DocumentUploaded,
                    Rules =
                    {
                        new JsonRuleDefinition
                        {
                            Name = "Inline",
                            Condition = "title == \"Report\"",
                            Set = new Dictionary<string, object>
                            {
                                ["TagIds"] = new[] { inlineTag }
                            }
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options, tempDirectory.FullName);
        var context = CreateContext(new Dictionary<string, string>());

        var uploadResults = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);
        var ocrResults = engine.Execute(TaggingRuleSetNames.OcrCompleted, context);

        Assert.Contains(inlineTag, GetTagIds(uploadResults));
        Assert.Contains(fileTag, GetTagIds(ocrResults));
    }

    [Fact]
    public void BuiltInRules_CreateTagNames()
    {
        var engine = new RuleEngine(new IRuleProvider[] { new BuiltInRuleProvider() }, new RuleEngineOptions
        {
            ThrowIfRuleSetNotFound = false
        });

        var context = CreateContext(
            new Dictionary<string, string> { ["extension"] = ".pdf" },
            occurredAt: new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero));

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);
        var tagNames = GetTagNames(result);

        Assert.Contains("Uploaded 2024-03-01", tagNames);
        Assert.Contains("Document", tagNames);
    }

    private static RuleEngine CreateEngine(TaggerRulesOptions options, string contentRoot)
    {
        var provider = new TaggerRuleProvider(
            new TestHostEnvironment(contentRoot),
            NullLogger<TaggerRuleProvider>.Instance,
            new TestOptionsMonitor<TaggerRulesOptions>(options));

        return new RuleEngine(new IRuleProvider[] { provider }, new RuleEngineOptions { ThrowIfRuleSetNotFound = false });
    }

    private static IRuleContext CreateContext(
        IDictionary<string, string> metadata,
        string title = "Report",
        string? summary = null,
        string? content = null,
        DateTimeOffset? occurredAt = null)
    {
        var builder = TaggingRuleContextBuilder.FromMetadata(
            Guid.NewGuid(),
            title,
            occurredAt ?? DateTimeOffset.UtcNow,
            summary,
            content,
            metadata);
        var factory = new RuleContextFactory();
        return factory.FromDictionary((IDictionary<string, object>)builder.Build());
    }

    private static IReadOnlyCollection<Guid> GetTagIds(RuleExecutionResult result)
    {
        if (result.Output.TryGetValue("TagIds", out var value) && value is IEnumerable<Guid> guidList)
        {
            return guidList.ToArray();
        }

        return Array.Empty<Guid>();
    }

    private static IReadOnlyCollection<string> GetTagNames(RuleExecutionResult result)
    {
        if (result.Output.TryGetValue("TagNames", out var value) && value is IEnumerable<string> names)
        {
            return names.ToArray();
        }

        return Array.Empty<string>();
    }
}
