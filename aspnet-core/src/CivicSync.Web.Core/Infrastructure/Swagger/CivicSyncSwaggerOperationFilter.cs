using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CivicSync.Web.Core.Infrastructure.Swagger;

public sealed class CivicSyncSwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var httpMethod = context.ApiDescription.HttpMethod;
        var relativePath = context.ApiDescription.RelativePath;
        if (string.IsNullOrWhiteSpace(httpMethod) || string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        CivicSyncSwaggerDocumentation.Apply(httpMethod, relativePath, operation);
    }
}
