using ECM.Modules.Document.Application.Documents;

namespace Microsoft.Extensions.DependencyInjection;

public static class DocumentApplicationModuleExtensions
{
    public static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<DocumentApplicationService>();
        return services;
    }
}
