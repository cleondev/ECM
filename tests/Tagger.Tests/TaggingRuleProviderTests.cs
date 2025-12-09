using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Tagger;
using Xunit;

namespace Tagger.Tests;

public class TaggingRuleProviderTests
{
    [Fact]
    public void FileSystemProvider_LoadsRulesFromJsonFiles()
    {
        var tagId = Guid.NewGuid();
        var tempDirectory = Directory.CreateTempSubdirectory();
        var tempFile = Path.Combine(tempDirectory.FullName, "rules.json");

        File.WriteAllText(tempFile, $$"{{\"rules\":[{{\"name\":\"from file\",\"tagId\":\"{tagId}\"}}]}}\n");

        var filesOptions = new TaggingRuleFilesOptions();
        filesOptions.Paths.Add(tempFile);

        var provider = new FileSystemTaggingRuleProvider(
            new TestHostEnvironment(tempDirectory.FullName),
            NullLogger<FileSystemTaggingRuleProvider>.Instance,
            new TestOptionsMonitor<TaggingRuleFilesOptions>(filesOptions));

        var rules = provider.GetRules();

        Assert.Contains(rules, rule => rule.TagId == tagId);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRoot)
        {
            ContentRootPath = contentRoot;
        }

        public string ApplicationName { get; set; } = "Tagger.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = null!;

        public string ContentRootPath { get; set; }

        public string EnvironmentName { get; set; } = Environments.Production;
    }
}
