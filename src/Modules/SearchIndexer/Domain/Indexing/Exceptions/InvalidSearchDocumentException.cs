using System;

namespace ECM.SearchIndexer.Domain.Indexing.Exceptions;

public sealed class InvalidSearchDocumentException : Exception
{
    public InvalidSearchDocumentException(string message)
        : base(message)
    {
    }
}
