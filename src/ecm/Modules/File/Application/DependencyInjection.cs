using ECM.Modules.File.Application.Files;

namespace Microsoft.Extensions.DependencyInjection;

public static class FileApplicationModuleExtensions
{
    public static IServiceCollection AddFileApplication(this IServiceCollection services)
    {
        services.AddScoped<FileApplicationService>();
        return services;
    }
}
