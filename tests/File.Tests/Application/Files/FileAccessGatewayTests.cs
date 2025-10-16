using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Files;
using ECM.File.Application.Files;
using Xunit;

namespace File.Tests.Application.Files;

public class FileAccessGatewayTests
{
    [Fact]
    public async Task GetDownloadLinkAsync_WithMissingStorageKey_ReturnsFailure()
    {
        var storage = new FakeFileStorage();
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetDownloadLinkAsync("   ", TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("StorageKeyRequired", result.Errors);
        Assert.False(storage.WasGetDownloadLinkCalled);
    }

    [Fact]
    public async Task GetDownloadLinkAsync_WithNonPositiveLifetime_ReturnsFailure()
    {
        var storage = new FakeFileStorage();
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetDownloadLinkAsync("file-key", TimeSpan.Zero, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("LifetimeMustBePositive", result.Errors);
        Assert.False(storage.WasGetDownloadLinkCalled);
    }

    [Fact]
    public async Task GetDownloadLinkAsync_WhenStorageReturnsNull_ReturnsFailure()
    {
        var storage = new FakeFileStorage
        {
            GetDownloadLinkAsyncHandler = (_, _, _) => Task.FromResult<Uri?>(null)
        };
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetDownloadLinkAsync("file-key", TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Errors);
        Assert.True(storage.WasGetDownloadLinkCalled);
    }

    [Fact]
    public async Task GetDownloadLinkAsync_WhenStorageReturnsUri_ReturnsSuccess()
    {
        var requestedStorageKey = string.Empty;
        var storage = new FakeFileStorage
        {
            GetDownloadLinkAsyncHandler = (key, _, _) =>
            {
                requestedStorageKey = key;
                return Task.FromResult<Uri?>(new Uri("https://files.test/download"));
            }
        };
        var gateway = new FileAccessGateway(storage);

        var lifetime = TimeSpan.FromMinutes(10);
        var before = DateTimeOffset.UtcNow;
        var result = await gateway.GetDownloadLinkAsync("file-key", lifetime, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.Equal("file-key", requestedStorageKey);
        var link = Assert.IsType<FileDownloadLink>(result.Value);
        Assert.Equal(new Uri("https://files.test/download"), link.Uri);
        Assert.InRange(link.ExpiresAtUtc, before + lifetime, after + lifetime);
    }

    [Fact]
    public async Task GetContentAsync_WithMissingStorageKey_ReturnsFailure()
    {
        var storage = new FakeFileStorage();
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetContentAsync(" ", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("StorageKeyRequired", result.Errors);
        Assert.False(storage.WasDownloadCalled);
    }

    [Fact]
    public async Task GetContentAsync_WhenStorageReturnsNull_ReturnsFailure()
    {
        var storage = new FakeFileStorage
        {
            DownloadAsyncHandler = (_, _) => Task.FromResult<FileDownload?>(null)
        };
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetContentAsync("file-key", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Errors);
        Assert.True(storage.WasDownloadCalled);
    }

    [Fact]
    public async Task GetContentAsync_WhenStorageReturnsDownload_ReturnsSuccess()
    {
        var download = new FileDownload([1, 2, 3], "application/pdf", "file.pdf", DateTimeOffset.UtcNow);
        var storage = new FakeFileStorage
        {
            DownloadAsyncHandler = (_, _) => Task.FromResult<FileDownload?>(download)
        };
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetContentAsync("file-key", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var content = Assert.IsType<FileContent>(result.Value);
        Assert.Equal(download.Content, content.Content);
        Assert.Equal(download.ContentType, content.ContentType);
        Assert.Equal(download.FileName, content.FileName);
        Assert.Equal(download.LastModifiedUtc, content.LastModifiedUtc);
    }

    [Fact]
    public async Task GetContentAsync_WhenContentTypeMissing_DefaultsToOctetStream()
    {
        var download = new FileDownload([1], string.Empty, null, null);
        var storage = new FakeFileStorage
        {
            DownloadAsyncHandler = (_, _) => Task.FromResult<FileDownload?>(download)
        };
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetContentAsync("file-key", CancellationToken.None);

        Assert.True(result.IsSuccess);
        var content = Assert.IsType<FileContent>(result.Value);
        Assert.Equal("application/octet-stream", content.ContentType);
    }

    [Fact]
    public async Task GetThumbnailAsync_WithMissingStorageKey_ReturnsFailure()
    {
        var storage = new FakeFileStorage();
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetThumbnailAsync("", 100, 100, null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("StorageKeyRequired", result.Errors);
        Assert.False(storage.WasDownloadThumbnailCalled);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-1, 50)]
    public async Task GetThumbnailAsync_WithInvalidDimensions_ReturnsFailure(int width, int height)
    {
        var storage = new FakeFileStorage();
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetThumbnailAsync("file-key", width, height, "contain", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("InvalidDimensions", result.Errors);
        Assert.False(storage.WasDownloadThumbnailCalled);
    }

    [Fact]
    public async Task GetThumbnailAsync_WhenStorageReturnsNull_ReturnsFailure()
    {
        var storage = new FakeFileStorage
        {
            DownloadThumbnailAsyncHandler = (_, _, _, _, _) => Task.FromResult<FileDownload?>(null)
        };
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetThumbnailAsync("file-key", 100, 100, "cover", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Errors);
        Assert.True(storage.WasDownloadThumbnailCalled);
    }

    [Fact]
    public async Task GetThumbnailAsync_WhenStorageReturnsDownload_ReturnsSuccessAndNormalizesFit()
    {
        string? capturedFit = null;
        var download = new FileDownload([1, 2], null!, "thumb.jpg", DateTimeOffset.UtcNow);
        var storage = new FakeFileStorage
        {
            DownloadThumbnailAsyncHandler = (key, width, height, fit, _) =>
            {
                capturedFit = fit;
                return Task.FromResult<FileDownload?>(download);
            }
        };
        var gateway = new FileAccessGateway(storage);

        var result = await gateway.GetThumbnailAsync("file-key", 120, 80, "  Contain  ", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("contain", capturedFit);
        var content = Assert.IsType<FileContent>(result.Value);
        Assert.Equal(download.Content, content.Content);
        Assert.Equal("image/jpeg", content.ContentType);
        Assert.Equal(download.FileName, content.FileName);
        Assert.Equal(download.LastModifiedUtc, content.LastModifiedUtc);
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public bool WasGetDownloadLinkCalled { get; private set; }

        public bool WasDownloadCalled { get; private set; }

        public bool WasDownloadThumbnailCalled { get; private set; }

        public Func<string, TimeSpan, CancellationToken, Task<Uri?>>? GetDownloadLinkAsyncHandler { get; set; }

        public Func<string, CancellationToken, Task<FileDownload?>>? DownloadAsyncHandler { get; set; }

        public Func<string, int, int, string, CancellationToken, Task<FileDownload?>>? DownloadThumbnailAsyncHandler { get; set; }

        public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Uri?> GetDownloadLinkAsync(string storageKey, TimeSpan lifetime, CancellationToken cancellationToken = default)
        {
            WasGetDownloadLinkCalled = true;
            if (GetDownloadLinkAsyncHandler is not null)
            {
                return GetDownloadLinkAsyncHandler(storageKey, lifetime, cancellationToken);
            }

            return Task.FromResult<Uri?>(null);
        }

        public Task<FileDownload?> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            WasDownloadCalled = true;
            if (DownloadAsyncHandler is not null)
            {
                return DownloadAsyncHandler(storageKey, cancellationToken);
            }

            return Task.FromResult<FileDownload?>(null);
        }

        public Task<FileDownload?> DownloadThumbnailAsync(
            string storageKey,
            int width,
            int height,
            string fit,
            CancellationToken cancellationToken = default)
        {
            WasDownloadThumbnailCalled = true;
            if (DownloadThumbnailAsyncHandler is not null)
            {
                return DownloadThumbnailAsyncHandler(storageKey, width, height, fit, cancellationToken);
            }

            return Task.FromResult<FileDownload?>(null);
        }
    }
}
