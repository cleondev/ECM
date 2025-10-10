using ECM.File.Domain.Files;
using ECM.File.Infrastructure.Files;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.File.Infrastructure;

public static class FileInfrastructureModuleExtensions
{
    public static IServiceCollection AddFileInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileRepository, InMemoryFileRepository>();
        return services;
    }
}
