using ECM.Ocr.Domain.Annotations;
using ECM.Ocr.Domain.Extractions;
using ECM.Ocr.Domain.Results;
using ECM.Ocr.Domain.Templates;
using Microsoft.EntityFrameworkCore;

namespace ECM.Ocr.Infrastructure.Persistence;

public sealed class OcrDbContext(DbContextOptions<OcrDbContext> options) : DbContext(options)
{
    public DbSet<OcrDocumentResult> Results => Set<OcrDocumentResult>();

    public DbSet<OcrPageText> PageTexts => Set<OcrPageText>();

    public DbSet<OcrAnnotation> Annotations => Set<OcrAnnotation>();

    public DbSet<OcrExtraction> Extractions => Set<OcrExtraction>();

    public DbSet<OcrTemplate> Templates => Set<OcrTemplate>();

    public DbSet<OcrFieldDefinition> FieldDefinitions => Set<OcrFieldDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ocr");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OcrDbContext).Assembly);
    }
}
