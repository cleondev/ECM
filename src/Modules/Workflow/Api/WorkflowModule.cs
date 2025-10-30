using ECM.Abstractions;
using ECM.Workflow.Api.Workflows;
using ECM.Workflow.Application;
using ECM.Workflow.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.Workflow.Api;

public sealed class WorkflowModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWorkflowApplication();
        services.AddWorkflowInfrastructure();
        services.ConfigureModuleSwagger(WorkflowSwagger.DocumentName, WorkflowSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapWorkflowEndpoints();
    }
}

internal static class WorkflowSwagger
{
    internal const string DocumentName = "workflow";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Workflow API",
        Version = "v1",
        Description = "Workflow orchestration operations for ECM documents."
    };
}
