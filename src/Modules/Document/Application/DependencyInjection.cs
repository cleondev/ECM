using ECM.Document.Application.Documents.Commands;
using ECM.Document.Application.Tags.Commands;
using ECM.Document.Application.Tags.Queries;
using ECM.Document.Application.UserContext;
using ECM.Document.Application.Shares;
using Microsoft.Extensions.DependencyInjection;
using Shared.Utilities.ShortCode;

namespace ECM.Document.Application;

public static class DocumentApplicationModuleExtensions
{
    public static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<IDocumentUserContextResolver, DocumentUserContextResolver>();
        services.AddScoped<CreateDocumentCommandHandler>();
        services.AddScoped<UploadDocumentCommandHandler>();
        services.AddScoped<DeleteDocumentCommandHandler>();
        services.AddScoped<UpdateDocumentCommandHandler>();
        services.AddScoped<CreateTagLabelCommandHandler>();
        services.AddScoped<UpdateTagLabelCommandHandler>();
        services.AddScoped<DeleteTagLabelCommandHandler>();
        services.AddScoped<CreateTagNamespaceCommandHandler>();
        services.AddScoped<UpdateTagNamespaceCommandHandler>();
        services.AddScoped<DeleteTagNamespaceCommandHandler>();
        services.AddScoped<AssignTagToDocumentCommandHandler>();
        services.AddScoped<RemoveTagFromDocumentCommandHandler>();
        services.AddScoped<ListTagLabelsQueryHandler>();
        services.AddScoped<ListTagNamespacesQueryHandler>();
        services.AddScoped<CreateShareLinkCommandHandler>();
        services.AddScoped<UpdateShareLinkCommandHandler>();
        services.AddScoped<ShareLinkService>();
        services.AddScoped<ShareAccessService>();
        services.AddSingleton<ShortCodeGenerator>();
        return services;
    }
}
