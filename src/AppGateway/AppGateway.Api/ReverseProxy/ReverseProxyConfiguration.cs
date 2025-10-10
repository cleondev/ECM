using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace AppGateway.Api.ReverseProxy;

public static class ReverseProxyConfiguration
{
    public static IReadOnlyDictionary<string, string> CreateDefaultRoutes(IConfiguration configuration)
    {
        var ecmBaseAddress = configuration.GetValue<string>("Services:Ecm") ?? "http://localhost:8080";

        return new Dictionary<string, string>
        {
            ["/ecm"] = ecmBaseAddress
        };
    }
}
