using System;

namespace ECM.SearchIndexer.Domain.Indexing.Exceptions;

public sealed class InvalidSearchDocumentException(string message) : Exception(message)
{
}
