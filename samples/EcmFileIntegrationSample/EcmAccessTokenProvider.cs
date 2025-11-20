using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace samples.EcmFileIntegrationSample;

public sealed class EcmAccessTokenProvider
{
    private readonly IOptions<EcmIntegrationOptions> _options;
    private readonly ILogger<EcmAccessTokenProvider> _logger;
    private readonly ITokenAcquisition? _tokenAcquisition;

    public EcmAccessTokenProvider(
        IOptions<EcmIntegrationOptions> options,
        ILogger<EcmAccessTokenProvider> logger,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _logger = logger;
        _tokenAcquisition = serviceProvider.GetService<ITokenAcquisition>();
    }

    public bool RequiresUserAuthentication => _options.Value.UseAzureSso
        && string.IsNullOrWhiteSpace(_options.Value.AccessToken)
        && !_options.Value.OnBehalf.Enabled;

    public bool HasConfiguredAccess => !string.IsNullOrWhiteSpace(_options.Value.AccessToken)
        || _options.Value.OnBehalf.Enabled
        || (_options.Value.UseAzureSso && _tokenAcquisition is not null);

    public bool UsingOnBehalfAuthentication => _options.Value.OnBehalf.Enabled;

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;

        if (!string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return options.AccessToken;
        }

        if (options.OnBehalf.Enabled)
        {
            return null;
        }

        if (!options.UseAzureSso)
        {
            return null;
        }

        if (_tokenAcquisition is null)
        {
            _logger.LogWarning(
                "Azure SSO is enabled but Microsoft Identity was not configured. Check AzureAd settings in appsettings.json.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.AuthenticationScope))
        {
            _logger.LogWarning("Azure SSO is enabled but Ecm:AuthenticationScope is missing.");
            return null;
        }

        try
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                [options.AuthenticationScope],
                cancellationToken: cancellationToken);
        }
        catch (MsalUiRequiredException)
        {
            _logger.LogWarning("User interaction is required to acquire an access token.");
            throw;
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "Failed to acquire access token for scope {Scope} from Azure AD.", options.AuthenticationScope);
            throw;
        }
    }
}
