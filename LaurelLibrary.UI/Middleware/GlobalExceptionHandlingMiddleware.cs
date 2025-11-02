using System.Net;
using System.Text.Json;
using LaurelLibrary.Domain.Exceptions;

namespace LaurelLibrary.UI.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger
    )
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case SubscriptionUpgradeRequiredException subscriptionEx:
                _logger.LogWarning(
                    subscriptionEx,
                    "Subscription upgrade required: {Message}",
                    subscriptionEx.Message
                );

                // Check if this is an AJAX request or API call
                if (
                    IsAjaxRequest(context.Request)
                    || context.Request.Path.StartsWithSegments("/api")
                )
                {
                    response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                    response.Message = "Subscription Upgrade Required";
                    response.Details = subscriptionEx.Message;
                    response.RedirectUrl = subscriptionEx.RedirectUrl;
                }
                else
                {
                    // For regular page requests, redirect to subscription page with message
                    context.Response.Redirect(
                        $"{subscriptionEx.RedirectUrl}?message={Uri.EscapeDataString(subscriptionEx.Message)}"
                    );
                    return;
                }
                break;

            case InvalidOperationException invalidOpEx:
                // Check if this is a subscription-related exception
                if (
                    invalidOpEx.Message.Contains("subscription", StringComparison.OrdinalIgnoreCase)
                    || invalidOpEx.Message.Contains("upgrade", StringComparison.OrdinalIgnoreCase)
                    || invalidOpEx.Message.Contains("plan", StringComparison.OrdinalIgnoreCase)
                )
                {
                    // For subscription-related exceptions, redirect to subscription page
                    _logger.LogWarning(
                        invalidOpEx,
                        "Subscription-related InvalidOperationException occurred: {Message}",
                        invalidOpEx.Message
                    );

                    // Check if this is an AJAX request or API call
                    if (
                        IsAjaxRequest(context.Request)
                        || context.Request.Path.StartsWithSegments("/api")
                    )
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        response.Message = "Subscription Upgrade Required";
                        response.Details = invalidOpEx.Message;
                        response.RedirectUrl = "/Subscription";
                    }
                    else
                    {
                        // For regular page requests, redirect to subscription page with message
                        context.Response.Redirect(
                            $"/Subscription?message={Uri.EscapeDataString(invalidOpEx.Message)}"
                        );
                        return;
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid Operation";
                    response.Details = invalidOpEx.Message;
                    _logger.LogWarning(
                        invalidOpEx,
                        "InvalidOperationException occurred: {Message}",
                        invalidOpEx.Message
                    );
                }
                break;

            case ArgumentNullException argNullEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Request";
                response.Details = $"Required parameter is missing: {argNullEx.ParamName}";
                _logger.LogWarning(
                    argNullEx,
                    "ArgumentNullException occurred: {ParamName}",
                    argNullEx.ParamName
                );
                break;

            case ArgumentException argEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid Request";
                response.Details = argEx.Message;
                _logger.LogWarning(argEx, "ArgumentException occurred: {Message}", argEx.Message);
                break;

            case UnauthorizedAccessException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized";
                response.Details = "You are not authorized to perform this action";
                _logger.LogWarning(unauthorizedEx, "UnauthorizedAccessException occurred");
                break;

            case FileNotFoundException fileNotFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "File Not Found";
                response.Details = "The requested file could not be found";
                _logger.LogWarning(
                    fileNotFoundEx,
                    "FileNotFoundException occurred: {FileName}",
                    fileNotFoundEx.FileName
                );
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "Internal Server Error";
                response.Details = "An unexpected error occurred. Please try again later.";
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        context.Response.StatusCode = response.StatusCode;

        // Check if this is an AJAX request or API call
        if (IsAjaxRequest(context.Request) || context.Request.Path.StartsWithSegments("/api"))
        {
            // Return JSON response for AJAX/API requests
            var jsonResponse = JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
            await context.Response.WriteAsync(jsonResponse);
        }
        else
        {
            // Redirect to error page for regular page requests
            context.Items["ErrorStatusCode"] = response.StatusCode;
            context.Items["ErrorMessage"] = response.Message;
            context.Items["ErrorDetails"] = response.Details;
            context.Items["Exception"] = exception;

            context.Response.Redirect("/Error");
        }
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return request.Headers.ContainsKey("X-Requested-With")
            && request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}
