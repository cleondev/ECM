using ECM.Modules.Signature.Domain.Requests;
using ECM.Modules.Signature.Infrastructure.Requests;

namespace Microsoft.Extensions.DependencyInjection;

public static class SignatureInfrastructureModuleExtensions
{
    public static IServiceCollection AddSignatureInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISignatureRequestRepository, InMemorySignatureRequestRepository>();
        return services;
    }
}
