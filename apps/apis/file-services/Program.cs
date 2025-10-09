using Amazon.Runtime;
using Amazon.S3;

using FileServices.Configuration;
using FileServices.Endpoints;
using FileServices.Services;

using Microsoft.Extensions.Options;

using ServiceDefaults;

namespace FileServices;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services
            .AddOptions<MinioOptions>()
            .Bind(builder.Configuration.GetSection(MinioOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _), "The MinIO endpoint must be a valid absolute URI.")
            .ValidateOnStart();

        builder.Services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
            var endpoint = new Uri(options.Endpoint);

            var config = new AmazonS3Config
            {
                ServiceURL = endpoint.ToString(),
                ForcePathStyle = true,
                UseHttp = endpoint.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase),
                AuthenticationRegion = options.Region,
            };

            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);

            return new AmazonS3Client(credentials, config);
        });

        builder.Services.AddSingleton<IFileStorageService, MinioFileStorageService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapDefaultEndpoints();
        app.MapFileEndpoints();

        app.Run();
    }
}
