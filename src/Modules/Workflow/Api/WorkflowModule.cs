using ECM.Abstractions;
using ECM.Workflow.Api.Workflows;
using ECM.Workflow.Application;
using ECM.Workflow.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Workflow.Api;

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
