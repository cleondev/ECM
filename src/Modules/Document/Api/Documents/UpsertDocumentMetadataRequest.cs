using System.Text.Json;

namespace ECM.Document.Api.Documents;

public sealed class UpsertDocumentMetadataRequest
{
    public JsonElement Data { get; init; }
}
