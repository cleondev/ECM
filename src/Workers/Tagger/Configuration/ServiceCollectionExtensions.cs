using Ecm.Sdk.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tagger.Configuration;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredEcmUserContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(EcmUserOptions.SectionName).Get<EcmUserOptions>()
            ?? new EcmUserOptions();

        var userKey = string.IsNullOrWhiteSpace(options.UserKey)
            ? new EcmUserOptions().UserKey
            : options.UserKey;

        ManualEcmUserContext.SetUserKey(userKey);

        return services.AddSingleton<IEcmUserContext, ManualEcmUserContext>();
    }
}
