namespace Shared.Contracts.Messaging;

public static class EventTopics
{
    public static class Document
    {
        public const string Uploaded = "ecm.document.uploaded";
    }

    public static class Ocr
    {
        public const string Completed = "ecm.ocr.completed";
    }
}
