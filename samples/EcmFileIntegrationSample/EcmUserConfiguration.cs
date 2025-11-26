using Microsoft.Extensions.Configuration;

using Ecm.Sdk;

namespace samples.EcmFileIntegrationSample;

public sealed class EcmUserConfiguration
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public EcmIntegrationOptions BuildOptions(EcmIntegrationOptions defaults)
    {
        var merged = new EcmIntegrationOptions();
        EcmUserOptionsConfigurator.Copy(defaults, merged);

        if (!string.IsNullOrWhiteSpace(Email))
        {
            merged.OnBehalfUserEmail = Email;
        }

        return merged;
    }
}

public sealed class EcmUserStore
{
    private readonly EcmIntegrationOptions _defaults;
    private readonly IReadOnlyList<EcmUserConfiguration> _users;
    private readonly EcmUserConfiguration _defaultUser;

    private EcmUserStore(EcmIntegrationOptions defaults, IReadOnlyList<EcmUserConfiguration> users)
    {
        _defaults = defaults;
        _users = users;
        _defaultUser = new EcmUserConfiguration
        {
            DisplayName = "Cấu hình mặc định",
            Email = defaults.OnBehalfUserEmail,
        };
    }

    public static EcmUserStore FromConfiguration(IConfiguration configuration)
    {
        var defaults = configuration.GetSection("Ecm").Get<EcmIntegrationOptions>() ?? new EcmIntegrationOptions();
        var configuredUsers = configuration.GetSection("EcmUsers").Get<EcmUserConfiguration[]>() ?? [];

        var normalizedUsers = configuredUsers
            .Where(user => !string.IsNullOrWhiteSpace(user.Email))
            .Select(user =>
            {
                user.DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email! : user.DisplayName;
                return user;
            })
            .ToArray();

        return new EcmUserStore(defaults, normalizedUsers);
    }

    public EcmUserConfiguration DefaultUser => _users.Count > 0 ? _users[0] : _defaultUser;

    public IReadOnlyCollection<EcmUserConfiguration> Users => _users.Count > 0 ? _users : new[] { DefaultUser };

    public EcmIntegrationOptions DefaultOptions => BuildOptions(DefaultUser);

    public EcmIntegrationOptions BuildOptions(EcmUserConfiguration user)
    {
        return user.BuildOptions(_defaults);
    }

    public EcmUserConfiguration GetUserByEmailOrDefault(string? email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var match = _users.FirstOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return DefaultUser;
    }
}
