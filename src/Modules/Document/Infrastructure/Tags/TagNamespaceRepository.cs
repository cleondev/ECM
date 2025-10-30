using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Infrastructure.Tags;

public sealed class TagNamespaceRepository(DocumentDbContext context) : ITagNamespaceRepository
{
    private readonly DocumentDbContext _context = context;

    public Task<TagNamespace?> GetAsync(Guid namespaceId, CancellationToken cancellationToken = default)
        => _context.TagNamespaces.FirstOrDefaultAsync(ns => ns.Id == namespaceId, cancellationToken);
}
