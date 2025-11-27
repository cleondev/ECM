using AppGateway.Api.Configuration;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

using Xunit;

namespace AppGateway.Api.Tests.Configuration;

public static class StaticFileRedirectHelperTests
{
    [Fact]
    public static void ResolveDirectoryRedirect_AppendsTrailingSlash_ForRootMountedPaths()
    {
        using var temp = new TemporaryWebRoot();
        temp.CreateDirectoryWithIndex("me");

        using var provider = new PhysicalFileProvider(temp.RootPath);

        var result = StaticFileRedirectHelper.ResolveDirectoryRedirect(
            new PathString("/me"),
            PathString.Empty,
            new QueryString("?foo=bar"),
            "/ecm",
            provider);

        result.Should().Be("/me/?foo=bar");
    }

    [Fact]
    public static void ResolveDirectoryRedirect_AppendsTrailingSlash_ForUiRequestPaths()
    {
        using var temp = new TemporaryWebRoot();
        temp.CreateDirectoryWithIndex("hello");

        using var provider = new PhysicalFileProvider(temp.RootPath);

        var result = StaticFileRedirectHelper.ResolveDirectoryRedirect(
            new PathString("/ecm/hello"),
            PathString.Empty,
            QueryString.Empty,
            "/ecm",
            provider);

        result.Should().Be("/ecm/hello/");
    }

    [Fact]
    public static void ResolveDirectoryRedirect_ReturnsNull_WhenDirectoryMissing()
    {
        using var temp = new TemporaryWebRoot();

        using var provider = new PhysicalFileProvider(temp.RootPath);

        var result = StaticFileRedirectHelper.ResolveDirectoryRedirect(
            new PathString("/unknown"),
            PathString.Empty,
            QueryString.Empty,
            "/ecm",
            provider);

        result.Should().BeNull();
    }

    [Fact]
    public static void ResolveDirectoryRedirect_ReturnsNull_WhenPathHasExtension()
    {
        using var temp = new TemporaryWebRoot();
        temp.CreateDirectoryWithIndex("me");

        using var provider = new PhysicalFileProvider(temp.RootPath);

        var result = StaticFileRedirectHelper.ResolveDirectoryRedirect(
            new PathString("/me.json"),
            PathString.Empty,
            QueryString.Empty,
            "/ecm",
            provider);

        result.Should().BeNull();
    }

    [Fact]
    public static void ResolveDirectoryRedirect_PreservesPathBase()
    {
        using var temp = new TemporaryWebRoot();
        temp.CreateDirectoryWithIndex("me");

        using var provider = new PhysicalFileProvider(temp.RootPath);

        var result = StaticFileRedirectHelper.ResolveDirectoryRedirect(
            new PathString("/me"),
            new PathString("/gateway"),
            QueryString.Empty,
            "/ecm",
            provider);

        result.Should().Be("/gateway/me/");
    }

    private sealed class TemporaryWebRoot : IDisposable
    {
        public TemporaryWebRoot()
        {
            RootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public void CreateDirectoryWithIndex(string relativePath)
        {
            var directory = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "index.html"), "<!doctype html>");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(RootPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests.
            }
        }
    }
}

