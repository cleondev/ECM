using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECM.Abstractions;

public static class ModuleSwaggerExtensions
{
    public static IServiceCollection ConfigureModuleSwagger(
        this IServiceCollection services,
        string documentName,
        OpenApiInfo documentInfo,
        Action<SwaggerGenOptions>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(documentName))
        {
            throw new ArgumentException("Document name must be provided.", nameof(documentName));
        }

        if (documentInfo is null)
        {
            throw new ArgumentNullException(nameof(documentInfo));
        }

        services.Configure<SwaggerGenOptions>(options =>
        {
            if (!options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey(documentName))
            {
                options.SwaggerDoc(documentName, documentInfo);
            }

            if (options.SwaggerGeneratorOptions.DocInclusionPredicate is null)
            {
                options.SwaggerGeneratorOptions.DocInclusionPredicate = (docName, apiDescription) =>
                {
                    var groupName = apiDescription.GroupName;

                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        return false;
                    }

                    return string.Equals(docName, groupName, StringComparison.OrdinalIgnoreCase);
                };
            }

            configure?.Invoke(options);
        });

        return services;
    }
}
