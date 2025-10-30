using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests.Queries;

public sealed record GetSignatureRequestsQuery(SignatureStatus? Status);
