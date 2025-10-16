using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Application.Workflows.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Workflow.Application;

public static class WorkflowApplicationModuleExtensions
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddScoped<StartWorkflowCommandHandler>();
        services.AddScoped<CancelWorkflowInstanceCommandHandler>();
        services.AddScoped<ClaimWorkflowTaskCommandHandler>();
        services.AddScoped<CompleteWorkflowTaskCommandHandler>();
        services.AddScoped<ReassignWorkflowTaskCommandHandler>();

        services.AddScoped<GetActiveWorkflowsQueryHandler>();
        services.AddScoped<GetWorkflowDefinitionsQueryHandler>();
        services.AddScoped<GetWorkflowDefinitionQueryHandler>();
        services.AddScoped<GetWorkflowInstanceQueryHandler>();
        services.AddScoped<GetWorkflowTasksQueryHandler>();
        services.AddScoped<GetWorkflowTaskQueryHandler>();
        return services;
    }
}
