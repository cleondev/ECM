using ECM.Signature.Application.Requests;
using ECM.Signature.Infrastructure.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Signature.Infrastructure;

public static class SignatureInfrastructureModuleExtensions
{
    public static IServiceCollection AddSignatureInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISignatureRequestRepository, InMemorySignatureRequestRepository>();
        return services;
    }
}
