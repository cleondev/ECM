using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.File.Application.Files;
using ECM.File.Domain.Files;
using Xunit;

namespace File.Tests.Application.Files;

public class FileApplicationServiceTests
{
    [Fact]
    public async Task UploadAsync_WithValidRequest_UploadsFileAndPersistsMetadata()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var repository = new FakeFileRepository();
        var storage = new FakeFileStorage();
        var storageKeyGenerator = new FakeStorageKeyGenerator();
        var service = new FileApplicationService(repository, storage, clock, storageKeyGenerator);

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var request = new FileUploadRequest("document.pdf", "application/pdf", stream.Length, stream);

        var result = await service.UploadAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(repository.Files);
        Assert.Single(storage.Uploads);
        Assert.Equal(storageKeyGenerator.Key, result.Value!.StorageKey);
        Assert.Equal("application/pdf", storage.Uploads[0].ContentType);
    }

    private sealed class FakeFileRepository : IFileRepository
    {
        public List<StoredFile> Files { get; } = [];

        public Task<StoredFile> AddAsync(StoredFile file, CancellationToken cancellationToken = default)
        {
            Files.Add(file);
            return Task.FromResult(file);
        }

        public Task<IReadOnlyCollection<StoredFile>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<StoredFile>>(Files);
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public List<(string StorageKey, string ContentType)> Uploads { get; } = [];

        public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            Uploads.Add((storageKey, contentType));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStorageKeyGenerator : IStorageKeyGenerator
    {
        public string Key { get; set; } = "custom-key.pdf";

        public string Generate(string fileName)
        {
            GeneratedFileName = fileName;
            return Key;
        }

        public string? GeneratedFileName { get; private set; }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
