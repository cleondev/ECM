using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Models;

namespace Ocr.Tests.TestDoubles;

internal sealed class FakeOcrProvider : IOcrProvider
{
    public StartOcrCommand? CapturedStartCommand { get; private set; }

    public CancellationToken CapturedStartCancellationToken { get; private set; }

    public StartOcrResult StartResult { get; set; } = StartOcrResult.Empty;

    public List<(string SampleId, string BoxId, string Value, CancellationToken CancellationToken)> SetBoxValueCalls { get; } = [];

    public Task<StartOcrResult> StartProcessingAsync(StartOcrCommand command, CancellationToken cancellationToken = default)
    {
        CapturedStartCommand = command;
        CapturedStartCancellationToken = cancellationToken;
        return Task.FromResult(StartResult);
    }

    public Task<OcrResult> GetSampleResultAsync(string sampleId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OcrResult> GetBoxingResultAsync(string sampleId, string boxingId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<OcrBoxesResult> ListBoxesAsync(string sampleId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task SetBoxValueAsync(string sampleId, string boxId, string value, CancellationToken cancellationToken = default)
    {
        SetBoxValueCalls.Add((sampleId, boxId, value, cancellationToken));
        return Task.CompletedTask;
    }
}
