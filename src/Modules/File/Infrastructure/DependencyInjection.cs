using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.File.Application.Files;
using ECM.File.Infrastructure.Files;
using ECM.File.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.File.Infrastructure;

public static class FileInfrastructureModuleExtensions
{
    public static IServiceCollection AddFileInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<FileStorageOptions>()
            .BindConfiguration("FileStorage")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<FileDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetRequiredConnectionStringForModule("File");

            options.UseNpgsql(connectionString, builder => builder.MigrationsAssembly(typeof(FileDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IFileRepository, EfFileRepository>();
        services.AddScoped<IFileStorage, S3FileStorage>();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageOptions>>().Value;
            var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
            var hasCustomServiceUrl = !string.IsNullOrWhiteSpace(options.ServiceUrl);
            var config = new AmazonS3Config
            {
                ForcePathStyle = options.ForcePathStyle,
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.ServiceURL = options.ServiceUrl;
                config.AuthenticationRegion = options.Region;
                config.UseHttp = options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }
            else if (!string.IsNullOrWhiteSpace(options.Region))
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
            }

            return new AmazonS3Client(credentials, config);
        });

        return services;
    }
}
