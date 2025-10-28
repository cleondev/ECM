using ECM.IAM.Domain.Relations;
using Xunit;

namespace IAM.Test.Domain.Relations;

public class AccessRelationTests
{
    [Fact]
    public void Create_WithValidValues_TrimsStrings()
    {
        var subjectId = Guid.NewGuid();
        var objectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var relation = AccessRelation.Create(" user ", subjectId, "  document  ", objectId, "  owner  ", createdAt, createdAt.AddMinutes(-5), createdAt.AddHours(1));

        Assert.Equal("user", relation.SubjectType);
        Assert.Equal(subjectId, relation.SubjectId);
        Assert.Equal("document", relation.ObjectType);
        Assert.Equal(objectId, relation.ObjectId);
        Assert.Equal("owner", relation.Relation);
        Assert.Equal(createdAt, relation.CreatedAtUtc);
        Assert.Equal(createdAt.AddMinutes(-5), relation.ValidFromUtc);
        Assert.Equal(createdAt.AddHours(1), relation.ValidToUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingObjectType_ThrowsArgumentException(string? objectType)
    {
        var exception = Assert.Throws<ArgumentException>(() => AccessRelation.Create(
            "user",
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
            "user",
            Guid.NewGuid(),
            "document",
            Guid.NewGuid(),
            relation!,
            DateTimeOffset.UtcNow));

        Assert.StartsWith("Relation is required.", exception.Message);
        Assert.Equal("relation", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("manager")]
    public void Create_WithInvalidSubjectType_ThrowsArgumentException(string? subjectType)
    {
        var exception = Assert.Throws<ArgumentException>(() => AccessRelation.Create(
            subjectType!,
            Guid.NewGuid(),
            "document",
            Guid.NewGuid(),
            "reader",
            DateTimeOffset.UtcNow));

        Assert.StartsWith("Subject type", exception.Message, StringComparison.Ordinal);
        Assert.Equal("subjectType", exception.ParamName);
    }

    [Fact]
    public void Create_WithInvalidValidityRange_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var exception = Assert.Throws<ArgumentException>(() => AccessRelation.Create(
            "group",
            Guid.NewGuid(),
            "document",
            Guid.NewGuid(),
            "reader",
            now,
            now,
            now.AddMinutes(-1)));

        Assert.Equal("validToUtc", exception.ParamName);
    }
}
