using CivicSync.Node.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Tests.Infrastructure;

public sealed class ApiKeyAuthenticationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenProtectedApiRequestHasNoApiKey()
    {
        var nextWasCalled = false;
        var context = CreateContext("/api/citizens");
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.False(nextWasCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenProtectedApiRequestHasValidApiKey()
    {
        var nextWasCalled = false;
        var context = CreateContext("/api/citizens");
        context.Request.Headers[ApiKeyAuthenticationMiddleware.HeaderName] = "test-api-key";
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SkipsApiKeyCheck_ForIncomingNodeSyncEndpoint()
    {
        var nextWasCalled = false;
        var context = CreateContext("/api/sync/ledger-entries");
        var middleware = CreateMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static ApiKeyAuthenticationMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ApiKeyAuthenticationMiddleware(
            next,
            Options.Create(new ApiKeyOptions { ApiKey = "test-api-key" }));
    }
}
