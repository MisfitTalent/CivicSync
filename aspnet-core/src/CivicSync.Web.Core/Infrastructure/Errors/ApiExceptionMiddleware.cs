using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CivicSync.Web.Core.Infrastructure.Errors;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled API exception.");
        }
        else
        {
            _logger.LogWarning(exception, "Handled API exception.");
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            InvalidOperationException invalidOperationException when IsNotFoundException(invalidOperationException) => StatusCodes.Status404NotFound,
            InvalidOperationException invalidOperationException when IsConflictException(invalidOperationException) => StatusCodes.Status409Conflict,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static bool IsNotFoundException(InvalidOperationException exception)
    {
        return exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConflictException(InvalidOperationException exception)
    {
        return exception.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Request could not be processed.",
            StatusCodes.Status404NotFound => "Resource was not found.",
            StatusCodes.Status409Conflict => "Request conflicts with the current resource state.",
            _ => "Unexpected server error."
        };
    }
}
