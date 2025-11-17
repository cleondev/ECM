using System;
using System.Collections.Generic;
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

        var engine = new TaggingRuleEngine(new TestOptionsMonitor(options));
        var context = TaggingRuleContext.Create(
            Guid.NewGuid(),
            "Benefits Package",
            "Summary",
            null,
            new Dictionary<string, string>
            {
                ["extension"] = ".PDF",
                ["uploadedBy"] = "Alice"
            });

        var result = engine.Evaluate(context, TaggingRuleTrigger.DocumentUploaded);

        Assert.Contains(tagId, result);
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

        var engine = new TaggingRuleEngine(new TestOptionsMonitor(options));
        var context = TaggingRuleContext.Create(Guid.NewGuid(), "Invoice", null, null, new Dictionary<string, string>
        {
            ["classification"] = "invoice"
        });

        var result = engine.Evaluate(context, TaggingRuleTrigger.DocumentUploaded);

        Assert.Empty(result);
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

        var engine = new TaggingRuleEngine(new TestOptionsMonitor(options));
        var context = TaggingRuleContext.Create(Guid.NewGuid(), "Report", null, "Highly confidential document.", new Dictionary<string, string>());

        var result = engine.Evaluate(context, TaggingRuleTrigger.DocumentUploaded);

        Assert.Contains(tagId, result);
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

        var engine = new TaggingRuleEngine(new TestOptionsMonitor(options));
        var context = TaggingRuleContext.Create(Guid.NewGuid(), "Ops Doc", null, null, new Dictionary<string, string>
        {
            ["groupIds"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa,bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
        });

        var result = engine.Evaluate(context, TaggingRuleTrigger.DocumentUploaded);

        Assert.Single(result);
        Assert.Contains(tagId, result);
    }
}
