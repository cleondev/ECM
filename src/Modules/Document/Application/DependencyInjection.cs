using ECM.Document.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Document.Application;

public static class DocumentApplicationModuleExtensions
{
    public static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<DocumentApplicationService>();
        return services;
    }
}
