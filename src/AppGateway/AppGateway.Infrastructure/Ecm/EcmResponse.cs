using System.Net;

namespace AppGateway.Infrastructure.Ecm;

public sealed record EcmResponse<T>(HttpStatusCode StatusCode, T? Payload)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
}
