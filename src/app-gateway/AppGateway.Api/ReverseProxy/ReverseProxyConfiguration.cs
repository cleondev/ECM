using System.Collections.Generic;

namespace AppGateway.Api.ReverseProxy;

public static class ReverseProxyConfiguration
{
    public static IReadOnlyDictionary<string, string> CreateDefaultRoutes() => new Dictionary<string, string>
    {
        ["/ecm"] = "http://localhost:8080"
    };
}
