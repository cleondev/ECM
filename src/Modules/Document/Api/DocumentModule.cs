using System;
using ECM.Abstractions;
using ECM.Document.Api.Documents;
using ECM.Document.Api.Tags;
using ECM.Document.Application;
using ECM.Document.Infrastructure;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace ECM.Document.Api;

public sealed class DocumentModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDocumentApplication();
        services.AddDocumentInfrastructure();
        services.AddOptions<DocumentUploadDefaultsOptions>()
            .BindConfiguration(DocumentUploadDefaultsOptions.SectionName);

        services.AddOptions<DocumentUploadLimitOptions>()
            .BindConfiguration(DocumentUploadLimitOptions.SectionName)
            .Validate(static options =>
            {
                try
                {
                    options.EnsureValid();
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }
            }, "Document upload limits must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<FormOptions>()
            .Configure<IOptions<DocumentUploadLimitOptions>>((options, uploadLimits) =>
            {
                var limits = uploadLimits.Value;

                options.MultipartBodyLengthLimit = limits.MultipartBodyLengthLimit;
                options.ValueLengthLimit = int.MaxValue;
                options.MemoryBufferThreshold = int.MaxValue;
            });

        services.AddOptions<KestrelServerOptions>()
            .Configure<IOptions<DocumentUploadLimitOptions>>((options, uploadLimits) =>
            {
                options.Limits.MaxRequestBodySize = uploadLimits.Value.MaxRequestBodySize;
            });
        services.ConfigureModuleSwagger(DocumentSwagger.DocumentName, DocumentSwagger.Info);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDocumentEndpoints();
        endpoints.MapTagEndpoints();
    }
}

internal static class DocumentSwagger
{
    internal const string DocumentName = "documents";

    internal static readonly OpenApiInfo Info = new()
    {
        Title = "Documents API",
        Version = "v1",
        Description = "Operations for managing ECM documents and their associated tags."
    };
}
