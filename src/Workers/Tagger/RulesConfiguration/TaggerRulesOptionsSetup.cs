using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Tagger;

internal sealed class TaggerRulesOptionsSetup : IConfigureOptions<TaggerRulesOptions>
{
    private readonly IHostEnvironment _environment;

    public TaggerRulesOptionsSetup(IHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public void Configure(TaggerRulesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var relativePath = Path.Combine("RulesConfiguration", "JsonRules", "CustomerTag.json");
        if (options.Files.Any(file => string.Equals(file, relativePath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var absolutePath = Path.Combine(_environment.ContentRootPath, relativePath);
        if (File.Exists(absolutePath))
        {
            options.Files.Add(relativePath);
        }
    }
}
