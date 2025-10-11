using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Application.Workflows.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Workflow.Application;

public static class WorkflowApplicationModuleExtensions
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddScoped<StartWorkflowCommandHandler>();
        services.AddScoped<GetActiveWorkflowsQueryHandler>();
        return services;
    }
}
