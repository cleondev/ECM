using System;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Infrastructure.Camunda;
using ECM.Workflow.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Workflow.Infrastructure;

public static class WorkflowInfrastructureModuleExtensions
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<CamundaOptions>()
            .BindConfiguration(CamundaOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Invalid Camunda base URL.");

        services.AddHttpClient<CamundaWorkflowRepository>();
        services.AddScoped<IWorkflowRepository>(static provider => provider.GetRequiredService<CamundaWorkflowRepository>());

        return services;
    }
}
