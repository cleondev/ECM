using ECM.Workflow.Domain.Instances;
using ECM.Workflow.Infrastructure.Instances;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Workflow.Infrastructure;

public static class WorkflowInfrastructureModuleExtensions
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();
        return services;
    }
}
