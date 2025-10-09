using System;
using System.Net.Mail;
using ECM.BuildingBlocks.Application;
using ECM.Modules.Signature.Domain.Requests;

namespace ECM.Modules.Signature.Application.Requests;

public sealed record CreateSignatureRequestCommand(Guid DocumentId, string SignerEmail)
{
    public OperationResult<SignatureRequest> ToDomain()
    {
        if (!MailAddress.TryCreate(SignerEmail, out _))
        {
            return OperationResult<SignatureRequest>.Failure("Invalid signer email");
        }

        var request = new SignatureRequest(Guid.NewGuid(), DocumentId, SignerEmail, SignatureStatus.Pending, DateTimeOffset.UtcNow);
        return OperationResult<SignatureRequest>.Success(request);
    }
}
