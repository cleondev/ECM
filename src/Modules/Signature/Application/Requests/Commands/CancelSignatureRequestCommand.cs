using System;

namespace ECM.Signature.Application.Requests.Commands;

public sealed record CancelSignatureRequestCommand(Guid RequestId);
