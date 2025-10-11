using ECM.Document.Application.Documents.Services;
using ECM.Document.Application.Tags;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Document.Application;

public static class DocumentApplicationModuleExtensions
{
    public static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<DocumentApplicationService>();
        services.AddScoped<DocumentUploadApplicationService>();
        services.AddScoped<CreateTagLabelCommandHandler>();
        services.AddScoped<DeleteTagLabelCommandHandler>();
        services.AddScoped<AssignTagToDocumentCommandHandler>();
        services.AddScoped<RemoveTagFromDocumentCommandHandler>();
        return services;
    }
}
