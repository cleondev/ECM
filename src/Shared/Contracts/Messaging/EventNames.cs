namespace Shared.Contracts.Messaging;

public static class EventNames
{
    public static class Document
    {
        public const string Uploaded = "ecm.document.uploaded";
        public const string Created = "ecm.document.created";
        public const string Updated = "ecm.document.updated";
        public const string Deleted = "ecm.document.deleted";
        public const string TagAssigned = "ecm.document.tag-assigned";
        public const string TagRemoved = "ecm.document.tag-removed";
    }

    public static class TagLabel
    {
        public const string Created = "ecm.tag-label.created";
        public const string Updated = "ecm.tag-label.updated";
        public const string Deleted = "ecm.tag-label.deleted";
    }

    public static class File
    {
        public const string Uploaded = "ecm.file.uploaded";
    }

    public static class Ocr
    {
        public const string Completed = "ecm.ocr.completed";
    }

    public static class Ops
    {
        public const string HeartbeatFailed = "ecm.ops.heartbeat-failed";
        public const string MaintenanceScheduled = "ecm.ops.maintenance-scheduled";
        public const string MaintenanceCompleted = "ecm.ops.maintenance-completed";
        public const string IncidentRaised = "ecm.ops.incident-raised";
        public const string IncidentResolved = "ecm.ops.incident-resolved";
    }
}
