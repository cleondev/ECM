using ECM.Document.Application.Documents.Commands;
using ECM.Document.Application.Tags.Commands;
using ECM.Document.Application.Tags.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Document.Application;

public static class DocumentApplicationModuleExtensions
{
    public static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateDocumentCommandHandler>();
        services.AddScoped<UploadDocumentCommandHandler>();
        services.AddScoped<CreateTagLabelCommandHandler>();
        services.AddScoped<UpdateTagLabelCommandHandler>();
        services.AddScoped<DeleteTagLabelCommandHandler>();
        services.AddScoped<AssignTagToDocumentCommandHandler>();
        services.AddScoped<RemoveTagFromDocumentCommandHandler>();
        services.AddScoped<ListTagLabelsQueryHandler>();
        return services;
    }
}
