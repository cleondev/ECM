using System;
using System.Linq;
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

    public Task<TagNamespace?> GetUserNamespaceAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        => _context.TagNamespaces.FirstOrDefaultAsync(
            ns => ns.Scope == "user" && ns.OwnerUserId == ownerUserId,
            cancellationToken);

    public async Task<TagNamespace> AddAsync(TagNamespace tagNamespace, CancellationToken cancellationToken = default)
    {
        await _context.TagNamespaces.AddAsync(tagNamespace, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tagNamespace;
    }

    public Task<TagNamespace[]> ListAsync(string? scope = null, CancellationToken cancellationToken = default)
    {
        IQueryable<TagNamespace> query = _context.TagNamespaces.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(scope) && !string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedScope = scope.Trim().ToLowerInvariant();
            query = query.Where(ns => ns.Scope == normalizedScope);
        }

        return query
            .OrderBy(ns => ns.Scope)
            .ThenBy(ns => ns.DisplayName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TagNamespace> UpdateAsync(TagNamespace tagNamespace, CancellationToken cancellationToken = default)
    {
        _context.TagNamespaces.Update(tagNamespace);
        await _context.SaveChangesAsync(cancellationToken);
        return tagNamespace;
    }

    public async Task RemoveAsync(TagNamespace tagNamespace, CancellationToken cancellationToken = default)
    {
        _context.TagNamespaces.Remove(tagNamespace);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
