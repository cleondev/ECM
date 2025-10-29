using System.Text.Json;

namespace ECM.Document.Api.Documents.Requests;

public sealed class UpsertDocumentMetadataRequest
{
    public JsonElement Data { get; init; }
}
