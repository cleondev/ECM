using ECM.Workflow.Application.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Workflow.Application;

public static class WorkflowApplicationModuleExtensions
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddScoped<WorkflowApplicationService>();
        return services;
    }
}
