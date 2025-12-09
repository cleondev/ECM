using System;
using System.Collections.Generic;
using System.Linq;
using Ecm.Rules.Abstractions;
using Ecm.Rules.Engine;
using Tagger;
using Xunit;

namespace Tagger.Tests;

public class TaggingRuleEngineTests
{
    [Fact]
    public void Evaluate_ReturnsTagWhenConditionsMatch()
    {
        var tagId = Guid.NewGuid();
        var options = new TaggingRulesOptions
        {
            Rules =
            {
                new TaggingRuleOptions
                {
                    Name = "PDF Uploads",
                    TagId = tagId,
                    Trigger = TaggingRuleTrigger.DocumentUploaded,
                    Conditions =
                    {
                        new TaggingRuleConditionOptions
                        {
                            Field = "extension",
                            Operator = TaggingRuleOperator.Equals,
                            Value = ".pdf"
                        },
                        new TaggingRuleConditionOptions
                        {
                            Field = "uploadedBy",
                            Operator = TaggingRuleOperator.Equals,
                            Value = "alice"
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options.Rules);
        var context = CreateContext(new Dictionary<string, string>
        {
            ["extension"] = ".PDF",
            ["uploadedBy"] = "Alice"
        });

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);

        Assert.Contains(tagId, GetTagIds(result));
    }

    [Fact]
    public void Evaluate_SkipsRulesWhenTriggerDoesNotMatch()
    {
        var options = new TaggingRulesOptions
        {
            Rules =
            {
                new TaggingRuleOptions
                {
                    Name = "OCR Only",
                    TagId = Guid.NewGuid(),
                    Trigger = TaggingRuleTrigger.OcrCompleted,
                    Conditions =
                    {
                        new TaggingRuleConditionOptions
                        {
                            Field = "classification",
                            Operator = TaggingRuleOperator.Equals,
                            Value = "invoice"
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options.Rules);
        var context = CreateContext(new Dictionary<string, string>
        {
            ["classification"] = "invoice"
        });

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);

        Assert.Empty(GetTagIds(result));
    }

    [Fact]
    public void Evaluate_HonorsAnyMatchMode()
    {
        var tagId = Guid.NewGuid();
        var options = new TaggingRulesOptions
        {
            Rules =
            {
                new TaggingRuleOptions
                {
                    Name = "High Value",
                    TagId = tagId,
                    Match = TaggingRuleMatchMode.Any,
                    Conditions =
                    {
                        new TaggingRuleConditionOptions
                        {
                            Field = "ownerId",
                            Operator = TaggingRuleOperator.Equals,
                            Value = Guid.NewGuid().ToString()
                        },
                        new TaggingRuleConditionOptions
                        {
                            Field = "content",
                            Operator = TaggingRuleOperator.Contains,
                            Value = "confidential"
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options.Rules);
        var context = CreateContext(new Dictionary<string, string>
        {
            ["content"] = "Highly confidential document."
        }, content: "Highly confidential document.");

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);

        Assert.Contains(tagId, GetTagIds(result));
    }

    [Fact]
    public void Evaluate_SupportsRegexConditions()
    {
        var tagId = Guid.NewGuid();
        var options = new TaggingRulesOptions
        {
            Rules =
            {
                new TaggingRuleOptions
                {
                    Name = "Region",
                    TagId = tagId,
                    Conditions =
                    {
                        new TaggingRuleConditionOptions
                        {
                            Field = "groupIds",
                            Operator = TaggingRuleOperator.Regex,
                            Value = "(?i)^[0-9a-f-]+,.*"
                        }
                    }
                }
            }
        };

        var engine = CreateEngine(options.Rules);
        var context = CreateContext(new Dictionary<string, string>
        {
            ["groupIds"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa,bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
        });

        var result = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);

        Assert.Single(GetTagIds(result));
        Assert.Contains(tagId, GetTagIds(result));
    }

    [Fact]
    public void Evaluate_MergesRulesFromMultipleSources()
    {
        var uploadTag = Guid.NewGuid();
        var ocrTag = Guid.NewGuid();

        var engine = CreateEngine(new[]
        {
            new TaggingRuleOptions
            {
                Name = "Uploads",
                TagId = uploadTag,
                Trigger = TaggingRuleTrigger.DocumentUploaded
            },
            new TaggingRuleOptions
            {
                Name = "OCR",
                TagId = ocrTag,
                Trigger = TaggingRuleTrigger.OcrCompleted
            }
        });

        var context = CreateContext(new Dictionary<string, string>());

        var uploadResults = engine.Execute(TaggingRuleSetNames.DocumentUploaded, context);
        var ocrResults = engine.Execute(TaggingRuleSetNames.OcrCompleted, context);

        Assert.Contains(uploadTag, GetTagIds(uploadResults));
        Assert.Contains(ocrTag, GetTagIds(ocrResults));
    }

    private static RuleEngine CreateEngine(IReadOnlyCollection<TaggingRuleOptions> rules)
    {
        var provider = new TaggingRuleProvider(new[] { new TestTaggingRuleSource(rules) });
        return new RuleEngine(new IRuleProvider[] { provider }, new RuleEngineOptions { ThrowIfRuleSetNotFound = false });
    }

    private static IRuleContext CreateContext(IDictionary<string, string> metadata, string title = "Report", string? summary = null, string? content = null)
    {
        var builder = TaggingRuleContextBuilder.FromMetadata(Guid.NewGuid(), title, summary, content, metadata);
        var factory = new RuleContextFactory();
        return factory.FromDictionary(builder.Build());
    }

    private static IReadOnlyCollection<Guid> GetTagIds(RuleExecutionResult result)
    {
        if (result.Output.TryGetValue("TagIds", out var value) && value is IEnumerable<Guid> guidList)
        {
            return guidList.ToArray();
        }

        return Array.Empty<Guid>();
    }
}
