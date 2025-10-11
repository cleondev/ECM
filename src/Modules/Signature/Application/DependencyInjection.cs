using ECM.Signature.Application.Requests.Commands;
using ECM.Signature.Application.Requests.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Signature.Application;

public static class SignatureApplicationModuleExtensions
{
    public static IServiceCollection AddSignatureApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateSignatureRequestCommandHandler>();
        services.AddScoped<GetPendingSignatureRequestsQueryHandler>();
        return services;
    }
}
