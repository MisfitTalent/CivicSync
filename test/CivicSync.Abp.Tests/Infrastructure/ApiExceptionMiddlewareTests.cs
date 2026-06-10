using System.Text.Json;
using CivicSync.Web.Core.Infrastructure.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace CivicSync.Web.Host.Tests.Infrastructure;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsNotFoundProblemDetails_WhenResourceDoesNotExist()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Citizen does not exist on this node."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal("Resource was not found.", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Citizen does not exist on this node.", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ReturnsBadRequestProblemDetails_WhenDomainRuleFails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Only approved change requests can be committed."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("Request could not be processed.", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Only approved change requests can be committed.", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ReturnsConflictProblemDetails_WhenCitizenVersionConflictOccurs()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Citizen record version conflict. Expected version 1, but current version is 2."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("Request conflicts with the current resource state.", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Citizen record version conflict. Expected version 1, but current version is 2.", document.RootElement.GetProperty("detail").GetString());
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);

        return await reader.ReadToEndAsync();
    }
}

