using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.File.Domain.Files;

namespace ECM.File.Application.Files;

public sealed class UploadFileCommandHandler(
    IFileRepository repository,
    IFileStorage storage,
    ISystemClock clock,
    IStorageKeyGenerator storageKeyGenerator) : IFileStorageGateway
{
    private readonly IFileRepository _repository = repository;
    private readonly IFileStorage _storage = storage;
    private readonly ISystemClock _clock = clock;
    private readonly IStorageKeyGenerator _storageKeyGenerator = storageKeyGenerator;

    public async Task<OperationResult<FileUploadResult>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storageKey = _storageKeyGenerator.Generate(request.FileName);
        var createdAtUtc = _clock.UtcNow;

        await _storage.UploadAsync(storageKey, request.Content, request.ContentType, cancellationToken);

        var storedFile = await _repository.AddAsync(StoredFile.Create(storageKey, legalHold: false, createdAtUtc), cancellationToken);

        var result = new FileUploadResult(storedFile.StorageKey, request.FileName, request.ContentType, request.Length, storedFile.CreatedAtUtc);
        return OperationResult<FileUploadResult>.Success(result);
    }

}
