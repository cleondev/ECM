namespace ECM.Workflow.Domain.Definitions;

public sealed class WorkflowDefinition(string id, string key, string name, int version)
{
    public string Id { get; } = id;

    public string Key { get; } = key;

    public string Name { get; } = name;

    public int Version { get; } = version;
}
