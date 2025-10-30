using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.File.Application.Files;
using ECM.File.Application.Shares;
using Microsoft.Extensions.DependencyInjection;
using Shared.Utilities.ShortCode;

namespace ECM.File.Application;

public static class FileApplicationModuleExtensions
{
    public static IServiceCollection AddFileApplication(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IStorageKeyGenerator, DefaultStorageKeyGenerator>();
        services.AddSingleton<ShortCodeGenerator>();
        services.AddScoped<GetRecentFilesQueryHandler>();
        services.AddScoped<UploadFileCommandHandler>();
        services.AddScoped<FileAccessGateway>();
        services.AddScoped<CreateShareLinkCommandHandler>();
        services.AddScoped<UpdateShareLinkCommandHandler>();
        services.AddScoped<ShareLinkService>();
        services.AddScoped<ShareAccessService>();
        services.AddScoped<IFileStorageGateway>(provider => provider.GetRequiredService<UploadFileCommandHandler>());
        services.AddScoped<IFileAccessGateway>(provider => provider.GetRequiredService<FileAccessGateway>());
        return services;
    }
}
