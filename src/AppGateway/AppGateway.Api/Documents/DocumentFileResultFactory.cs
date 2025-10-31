using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Documents;

internal static class DocumentFileResultFactory
{
    public static FileContentResult Create(DocumentFileContent file)
    {
        return new FileContentResult(file.Content, file.ContentType)
        {
            FileDownloadName = file.FileName,
            LastModified = file.LastModifiedUtc,
            EnableRangeProcessing = file.EnableRangeProcessing,
        };
    }
}
