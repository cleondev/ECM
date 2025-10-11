using ECM.AccessControl.Domain.Relations;
using Xunit;

namespace AccessControl.Test.Domain.Relations;

public class AccessRelationTests
{
    [Fact]
    public void Create_WithValidValues_TrimsStrings()
    {
        var subjectId = Guid.NewGuid();
        var objectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var relation = AccessRelation.Create(subjectId, "  document  ", objectId, "  owner  ", createdAt);

        Assert.Equal(subjectId, relation.SubjectId);
        Assert.Equal("document", relation.ObjectType);
        Assert.Equal(objectId, relation.ObjectId);
        Assert.Equal("owner", relation.Relation);
        Assert.Equal(createdAt, relation.CreatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingObjectType_ThrowsArgumentException(string? objectType)
    {
        var exception = Assert.Throws<ArgumentException>(() => AccessRelation.Create(
            Guid.NewGuid(),
            objectType!,
            Guid.NewGuid(),
            "reader",
            DateTimeOffset.UtcNow));

        Assert.StartsWith("Object type is required.", exception.Message);
        Assert.Equal("objectType", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingRelation_ThrowsArgumentException(string? relation)
    {
        var exception = Assert.Throws<ArgumentException>(() => AccessRelation.Create(
            Guid.NewGuid(),
            "document",
            Guid.NewGuid(),
            relation!,
            DateTimeOffset.UtcNow));

        Assert.StartsWith("Relation is required.", exception.Message);
        Assert.Equal("relation", exception.ParamName);
    }
}
