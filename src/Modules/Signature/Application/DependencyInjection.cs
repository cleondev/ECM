using ECM.Signature.Application.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Signature.Application;

public static class SignatureApplicationModuleExtensions
{
    public static IServiceCollection AddSignatureApplication(this IServiceCollection services)
    {
        services.AddScoped<SignatureApplicationService>();
        return services;
    }
}
