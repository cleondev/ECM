using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.File.Domain.Files;

namespace ECM.File.Application.Files;

public sealed class UploadFileCommandHandler : IFileStorageGateway
{
    private readonly IFileRepository _repository;
    private readonly IFileStorage _storage;
    private readonly ISystemClock _clock;
    private readonly IStorageKeyGenerator _storageKeyGenerator;

    public UploadFileCommandHandler(
        IFileRepository repository,
        IFileStorage storage,
        ISystemClock clock,
        IStorageKeyGenerator storageKeyGenerator)
    {
        _repository = repository;
        _storage = storage;
        _clock = clock;
        _storageKeyGenerator = storageKeyGenerator;
    }

    public async Task<OperationResult<FileUploadResult>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var storageKey = _storageKeyGenerator.Generate(request.FileName);
        var createdAtUtc = _clock.UtcNow;

        await _storage.UploadAsync(storageKey, request.Content, request.ContentType, cancellationToken);

        var storedFile = await _repository.AddAsync(StoredFile.Create(storageKey, legalHold: false, createdAtUtc), cancellationToken);

        var result = new FileUploadResult(storedFile.StorageKey, request.FileName, request.ContentType, request.Length, storedFile.CreatedAtUtc);
        return OperationResult<FileUploadResult>.Success(result);
    }

}
