namespace ECM.Ocr.Application.Commands;

public sealed record SetOcrBoxValueCommand(string SampleId, string BoxId, string Value);
