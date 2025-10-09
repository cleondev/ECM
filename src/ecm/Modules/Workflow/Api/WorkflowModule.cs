using ECM.Modules.Abstractions;
using ECM.Modules.Workflow.Api.Workflows;
using ECM.Modules.Workflow.Application;
using ECM.Modules.Workflow.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Modules.Workflow.Api;

public sealed class WorkflowModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWorkflowApplication();
        services.AddWorkflowInfrastructure();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapWorkflowEndpoints();
    }
}
