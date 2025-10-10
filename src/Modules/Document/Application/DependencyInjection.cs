using ECM.Document.Application.Documents;
using ECM.Document.Application.Tags;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Document.Application;

public static class DocumentApplicationModuleExtensions
{
    public static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<DocumentApplicationService>();
        services.AddScoped<TagApplicationService>();
        return services;
    }
}
