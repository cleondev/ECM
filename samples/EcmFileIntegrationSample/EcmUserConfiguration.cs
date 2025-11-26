using Microsoft.Extensions.Configuration;

using Ecm.Sdk;

namespace samples.EcmFileIntegrationSample;

public sealed class EcmUserConfiguration
{
    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public EcmIntegrationOptions Settings { get; set; } = new();
}

public sealed class EcmUserStore
{
    private readonly IReadOnlyList<EcmUserConfiguration> _users;
    private readonly EcmUserConfiguration _defaultUser;

    private EcmUserStore(IReadOnlyList<EcmUserConfiguration> users, EcmUserConfiguration defaultUser)
    {
        _users = users;
        _defaultUser = defaultUser;
    }

    public static EcmUserStore FromConfiguration(IConfiguration configuration)
    {
        var configuredUsers = configuration.GetSection("EcmUsers").Get<EcmUserConfiguration[]>() ?? [];

        var defaultSettings = configuration.GetSection("Ecm").Get<EcmIntegrationOptions>() ?? new EcmIntegrationOptions();

        var defaultUser = new EcmUserConfiguration
        {
            Key = "default",
            DisplayName = "Cấu hình mặc định",
            Settings = defaultSettings,
        };

        var normalizedUsers = configuredUsers
            .Where(user => !string.IsNullOrWhiteSpace(user.Key))
            .ToArray();

        return new EcmUserStore(normalizedUsers, defaultUser);
    }

    public EcmUserConfiguration DefaultUser => _users.Count > 0 ? _users[0] : _defaultUser;

    public IReadOnlyCollection<EcmUserConfiguration> Users => _users.Count > 0 ? _users : new[] { DefaultUser };

    public EcmUserConfiguration GetUserOrDefault(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            var match = _users.FirstOrDefault(user => string.Equals(user.Key, key, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return DefaultUser;
    }
}
