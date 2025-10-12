namespace ECM.Workflow.Domain.Definitions;

public sealed class WorkflowDefinition
{
    public WorkflowDefinition(string id, string key, string name, int version)
    {
        Id = id;
        Key = key;
        Name = name;
        Version = version;
    }

    public string Id { get; }

    public string Key { get; }

    public string Name { get; }

    public int Version { get; }
}
