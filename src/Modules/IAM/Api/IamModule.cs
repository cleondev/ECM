using ECM.Abstractions;
using ECM.Abstractions.Users;
using ECM.IAM.Api.Auth;
using ECM.IAM.Api.Relations;
using ECM.IAM.Api.Roles;
using ECM.IAM.Api.Users;
using ECM.IAM.Application;
using ECM.IAM.Application.Users;
using ECM.IAM.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ECM.IAM.Api;

public sealed class IamModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddIamApplication();
        services.AddIamInfrastructure();
        services.ConfigureModuleSwagger(IamSwagger.DocumentName, IamSwagger.Info);
        services.AddOptions<IamProvisioningOptions>().BindConfiguration(IamProvisioningOptions.SectionName);
        services.AddScoped<IUserProvisioningService, AzureAdUserProvisioningService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUserEndpoints();
        endpoints.MapUserProfileEndpoints();
        endpoints.MapAuthenticationEndpoints();
        endpoints.MapRoleEndpoints();
        endpoints.MapRelationEndpoints();
    }
}

internal static class IamSwagger
{
    internal const string DocumentName = "iam";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "IAM API",
        Version = "v1",
        Description = "Endpoints that manage users, roles, relations and permissions."
    };
}
