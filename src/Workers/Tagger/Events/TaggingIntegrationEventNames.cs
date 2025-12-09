using System;

namespace Tagger;

internal static class TaggingIntegrationEventNames
{
    public const string DocumentUploaded = "DocumentUploaded";
    public const string OcrCompleted = "OcrCompleted";

    public static string FromEvent(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent switch
        {
            DocumentUploadedIntegrationEvent => DocumentUploaded,
            OcrCompletedIntegrationEvent => OcrCompleted,
            _ => integrationEvent.GetType().Name
        };
    }
}
