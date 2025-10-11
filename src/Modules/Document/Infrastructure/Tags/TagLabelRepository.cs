using ECM.Document.Application.Tags;
using ECM.Document.Domain.Tags;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Infrastructure.Tags;

public sealed class TagLabelRepository(DocumentDbContext context) : ITagLabelRepository
{
    private readonly DocumentDbContext _context = context;

    public Task<TagLabel?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
        => _context.TagLabels.FirstOrDefaultAsync(label => label.Id == tagId, cancellationToken);

    public Task<TagLabel?> GetByNamespaceAndPathAsync(string namespaceSlug, string path, CancellationToken cancellationToken = default)
        => _context.TagLabels.FirstOrDefaultAsync(label => label.NamespaceSlug == namespaceSlug && label.Path == path, cancellationToken);

    public Task<bool> NamespaceExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default)
        => _context.TagNamespaces.AnyAsync(ns => ns.NamespaceSlug == namespaceSlug, cancellationToken);

    public async Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        await _context.TagLabels.AddAsync(tagLabel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tagLabel;
    }

    public async Task RemoveAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        _context.TagLabels.Remove(tagLabel);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
