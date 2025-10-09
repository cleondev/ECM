using ECM.Modules.Workflow.Domain.Instances;
using ECM.Modules.Workflow.Infrastructure.Instances;

namespace Microsoft.Extensions.DependencyInjection;

public static class WorkflowInfrastructureModuleExtensions
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();
        return services;
    }
}
