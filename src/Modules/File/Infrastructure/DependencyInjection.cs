using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.File.Application.Files;
using ECM.File.Infrastructure.Files;
using ECM.File.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Minio;

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
        services.AddScoped<IFileStorage>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

            return options.Provider switch
            {
                FileStorageProvider.AwsS3 => ActivatorUtilities.CreateInstance<S3FileStorage>(serviceProvider),
                FileStorageProvider.Minio => ActivatorUtilities.CreateInstance<MinioFileStorage>(serviceProvider),
                _ => throw new InvalidOperationException($"Unsupported file storage provider '{options.Provider}'."),
            };
        });

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
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

        services.AddSingleton<IMinioClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

            if (options.Provider != FileStorageProvider.Minio)
            {
                throw new InvalidOperationException("MinIO client is only available when the file storage provider is set to Minio.");
            }

            return CreateMinioClient(options);
        });
        return services;
    }

    private static IMinioClient CreateMinioClient(FileStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            throw new InvalidOperationException("FileStorage:ServiceUrl must be configured when using the Minio provider.");
        }

        var builder = new MinioClient();
        var endpoint = options.ServiceUrl.Trim();

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            var secure = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            var host = uri.Host;
            if (!uri.IsDefaultPort)
            {
                host = $"{host}:{uri.Port}";
            }

            builder = (MinioClient)builder.WithEndpoint(host);

            if (secure)
            {
                builder = (MinioClient)builder.WithSSL();
            }
        }
        else
        {
            builder = (MinioClient)builder.WithEndpoint(endpoint);
        }

        builder = (MinioClient)builder.WithCredentials(options.AccessKeyId, options.SecretAccessKey);

        return builder.Build();
    }
}
