using ECM.Modules.Workflow.Application.Workflows;

namespace Microsoft.Extensions.DependencyInjection;

public static class WorkflowApplicationModuleExtensions
{
    public static IServiceCollection AddWorkflowApplication(this IServiceCollection services)
    {
        services.AddScoped<WorkflowApplicationService>();
        return services;
    }
}
