using ECM.File.Application.Files;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.File.Application;

public static class FileApplicationModuleExtensions
{
    public static IServiceCollection AddFileApplication(this IServiceCollection services)
    {
        services.AddScoped<FileApplicationService>();
        return services;
    }
}
