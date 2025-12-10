using Ecm.Rules.Providers.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Tagger.Rules.Configuration;

using Xunit;

namespace Tagger.Tests;

public class TaggingRuleProviderTests
{
    [Fact]
    public void FileSource_LoadsRulesFromJsonFiles()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var ruleFile = Path.Combine(tempDirectory.FullName, "rules.json");
        var tagId = Guid.NewGuid();

        File.WriteAllText(
            ruleFile,
            $$"""
            [
              {
                "name": "{TaggingRuleSetNames.DocumentUploaded}",
                "rules": [
                  {
                    "name": "From File",
                    "condition": "extension == \".pdf\"",
                    "set": { "TagIds": ["{tagId}"] }
                  }
                ]
              }
            ]
            """
        );

        var options = new TaggerRulesOptions
        {
            Files = { ruleFile }
        };

        var provider = new TaggerRuleProvider(
            new TestHostEnvironment(tempDirectory.FullName),
            NullLogger<TaggerRuleProvider>.Instance,
            new TestOptionsMonitor<TaggerRulesOptions>(options));

        var ruleSets = provider.GetRuleSets().ToArray();

        Assert.Contains(ruleSets, set => set.Name == TaggingRuleSetNames.DocumentUploaded);
        Assert.Single(ruleSets.Single(set => set.Name == TaggingRuleSetNames.DocumentUploaded).Rules);
    }

    [Fact]
    public void InlineAndFileDefinitionsAreMerged()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var ruleFile = Path.Combine(tempDirectory.FullName, "rules.json");

        File.WriteAllText(
            ruleFile,
            $$"""
            [
              {
                "name": "{TaggingRuleSetNames.DocumentUploaded}",
                "rules": [
                  { "name": "From File", "condition": "title == \"Report\"", "set": { "TagIds": ["{Guid.NewGuid()}"] } }
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
                            Condition = "summary == \"draft\"",
                            Set = new() { ["TagIds"] = new[] { Guid.NewGuid() } }
                        }
                    }
                }
            }
        };

        var provider = new TaggerRuleProvider(
            new TestHostEnvironment(tempDirectory.FullName),
            NullLogger<TaggerRuleProvider>.Instance,
            new TestOptionsMonitor<TaggerRulesOptions>(options));

        var ruleSets = provider.GetRuleSets().ToArray();
        var documentRules = ruleSets.Single(set => set.Name == TaggingRuleSetNames.DocumentUploaded);

        Assert.Equal(2, documentRules.Rules.Count);
    }
}
