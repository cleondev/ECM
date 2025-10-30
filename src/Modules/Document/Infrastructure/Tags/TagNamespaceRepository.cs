using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ECM.Document.Infrastructure.Tags;

public sealed class TagNamespaceRepository(DocumentDbContext context) : ITagNamespaceRepository
{
    private readonly DocumentDbContext _context = context;

    public Task<bool> ExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default)
        => _context.TagNamespaces.AnyAsync(ns => ns.NamespaceSlug == namespaceSlug, cancellationToken);

    public async Task EnsureUserNamespaceAsync(
        string namespaceSlug,
        Guid? ownerUserId,
        string? displayName,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (await ExistsAsync(namespaceSlug, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var tagNamespace = new TagNamespace(namespaceSlug, "user", ownerUserId, displayName, createdAtUtc);
        await _context.TagNamespaces.AddAsync(tagNamespace, cancellationToken).ConfigureAwait(false);

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _context.Entry(tagNamespace).State = EntityState.Detached;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            return postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
        }

        if (exception.GetBaseException() is PostgresException basePostgresException)
        {
            return basePostgresException.SqlState == PostgresErrorCodes.UniqueViolation;
        }

        return false;
    }
}
