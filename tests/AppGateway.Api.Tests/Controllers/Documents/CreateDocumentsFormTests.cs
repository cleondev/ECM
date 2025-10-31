using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AppGateway.Api.Controllers.Documents;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

using Xunit;

namespace AppGateway.Api.Tests.Controllers.Documents;

public sealed class CreateDocumentsFormTests
{
    [Fact]
    public async Task BindAsync_ParsesTagIdsFromArrayFields()
    {
        // Arrange
        var tagIdA = Guid.NewGuid();
        var tagIdB = Guid.NewGuid();

        var formValues = new Dictionary<string, StringValues>
        {
            ["Tags[]"] = new StringValues(new[] { tagIdA.ToString(), tagIdB.ToString() }),
        };

        var form = new FormCollection(formValues);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "multipart/form-data; boundary=----TestBoundary";
        context.Features.Set<IFormFeature>(new FormFeature(form));

        // Act
        var result = await CreateDocumentsForm.BindAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Collection(
            result!.TagIds,
            value => Assert.Equal(tagIdA, value),
            value => Assert.Equal(tagIdB, value));
    }

    [Fact]
    public async Task BindAsync_IgnoresJsonEncodedTagsField()
    {
        // Arrange
        var tagId = Guid.NewGuid();

        var formValues = new Dictionary<string, StringValues>
        {
            ["Tags"] = new StringValues($"[\"{tagId}\"]"),
        };

        var form = new FormCollection(formValues);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "multipart/form-data; boundary=----TestBoundary";
        context.Features.Set<IFormFeature>(new FormFeature(form));

        // Act
        var result = await CreateDocumentsForm.BindAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result!.TagIds);
    }
}
