using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests;

public sealed record SignatureRequestQuery(SignatureStatus? Status);
