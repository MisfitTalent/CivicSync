using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Core.Infrastructure.Security;

public sealed class ApiKeyAuthenticationMiddleware
{
    public const string HeaderName = "X-CivicSync-Api-Key";

    private readonly RequestDelegate _next;
    private readonly ApiKeyOptions _options;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IOptions<ApiKeyOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipApiKeyCheck(context.Request))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("CivicSync API key authentication is not configured.");
            return;
        }

        var providedApiKey = context.Request.Headers[HeaderName].ToString();
        if (!IsValidApiKey(providedApiKey, _options.ApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("CivicSync API key is missing or invalid.");
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkipApiKeyCheck(HttpRequest request)
    {
        return !request.Path.StartsWithSegments("/api") ||
               request.Path.StartsWithSegments("/api/sync/ledger-entries") ||
               request.Path.StartsWithSegments("/swagger") ||
               request.Path.StartsWithSegments("/openapi");
    }

    private static bool IsValidApiKey(string providedApiKey, string configuredApiKey)
    {
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredApiKey);

        return providedBytes.Length == configuredBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }
}
