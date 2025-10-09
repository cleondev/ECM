using ECM.Modules.Signature.Application.Requests;

namespace Microsoft.Extensions.DependencyInjection;

public static class SignatureApplicationModuleExtensions
{
    public static IServiceCollection AddSignatureApplication(this IServiceCollection services)
    {
        services.AddScoped<SignatureApplicationService>();
        return services;
    }
}
