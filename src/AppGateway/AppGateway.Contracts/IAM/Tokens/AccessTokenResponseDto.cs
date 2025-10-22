using System;

namespace AppGateway.Contracts.IAM.Tokens;

public sealed record AccessTokenResponseDto(string AccessToken, DateTimeOffset ExpiresOn);
