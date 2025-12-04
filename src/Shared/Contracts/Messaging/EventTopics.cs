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

    public static class File
    {
        public const string Events = "file.events";
    }

    public static class Iam
    {
        public const string Events = "iam.events";
    }

    public static class Ocr
    {
        public const string Events = "ocr.events";
    }

    public static class Ops
    {
        public const string Events = "ops.events";
    }

    public static class Tags
    {
        public const string Events = "tag.events";
    }

    public static class Webhooks
    {
        public const string Events = "webhook-requests";
    }
}
