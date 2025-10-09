using ECM.Modules.File.Domain.Files;
using ECM.Modules.File.Infrastructure.Files;

namespace Microsoft.Extensions.DependencyInjection;

public static class FileInfrastructureModuleExtensions
{
    public static IServiceCollection AddFileInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileRepository, InMemoryFileRepository>();
        return services;
    }
}
