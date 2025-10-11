using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.File.Application.Files;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.File.Application;

public static class FileApplicationModuleExtensions
{
    public static IServiceCollection AddFileApplication(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IStorageKeyGenerator, DefaultStorageKeyGenerator>();
        services.AddScoped<FileApplicationService>();
        services.AddScoped<IFileUploadService>(provider => provider.GetRequiredService<FileApplicationService>());
        return services;
    }
}
