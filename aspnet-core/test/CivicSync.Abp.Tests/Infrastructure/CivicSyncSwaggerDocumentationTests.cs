using CivicSync.Web.Core.Infrastructure.Swagger;
using Microsoft.OpenApi;

namespace CivicSync.Web.Host.Tests.Infrastructure;

public sealed class CivicSyncSwaggerDocumentationTests
{
    [Fact]
    public void Apply_SetsSummaryAndDescription_ForCommitEndpoint()
    {
        var operation = new OpenApiOperation();

        CivicSyncSwaggerDocumentation.Apply("POST", "api/change-requests/{id}/commit", operation);

        Assert.Equal("Commit an approved change request", operation.Summary);
        Assert.Contains("append-only ledger entry", operation.Description);
    }

    [Fact]
    public void Apply_LeavesOperationUnchanged_ForUnknownEndpoint()
    {
        var operation = new OpenApiOperation
        {
            Summary = "Existing summary",
            Description = "Existing description"
        };

        CivicSyncSwaggerDocumentation.Apply("GET", "api/unknown", operation);

        Assert.Equal("Existing summary", operation.Summary);
        Assert.Equal("Existing description", operation.Description);
    }
}
