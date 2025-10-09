using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Modules.Abstractions;

public interface IModule
{
    void ConfigureServices(IServiceCollection services);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public static class ModuleRegistrationExtensions
{
    public static WebApplicationBuilder AddModule<TModule>(this WebApplicationBuilder builder)
        where TModule : class, IModule, new()
    {
        var module = new TModule();
        module.ConfigureServices(builder.Services);
        builder.Services.AddSingleton<IModule>(module);

        return builder;
    }

    public static WebApplication MapModules(this WebApplication app)
    {
        var modules = app.Services.GetRequiredService<IEnumerable<IModule>>();

        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}
