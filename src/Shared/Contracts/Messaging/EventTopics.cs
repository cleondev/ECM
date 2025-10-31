namespace Shared.Contracts.Messaging;

public static class EventTopics
{
    /// <summary>
    ///     Topics that aggregate module domain events. Individual consumers should inspect the message type
    ///     (for example <c>ecm.document.updated</c>) inside the envelope when they share the same topic.
    /// </summary>
    public static class Document
    {
        public const string Events = "document.events";
    }

    public static class Iam
    {
        public const string Events = "iam.events";
    }

    /// <summary>
    ///     Topics that are dedicated to long-running pipeline hand-offs where we want a 1:1 mapping between
    ///     topic and integration event type (e.g., upload → OCR → search indexing).
    /// </summary>
    public static class Pipelines
    {
        public static class Document
        {
            public const string Uploaded = EventNames.Document.Uploaded;
        }

        public static class Ocr
        {
            public const string Completed = EventNames.Ocr.Completed;
        }
    }
}
