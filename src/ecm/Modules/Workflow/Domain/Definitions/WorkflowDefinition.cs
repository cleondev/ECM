using System;
using System.Collections.Generic;

namespace ECM.Modules.Workflow.Domain.Definitions;

public sealed class WorkflowDefinition
{
    public WorkflowDefinition(Guid id, string name, IReadOnlyList<string> steps)
    {
        Id = id;
        Name = name;
        Steps = steps;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<string> Steps { get; }
}
