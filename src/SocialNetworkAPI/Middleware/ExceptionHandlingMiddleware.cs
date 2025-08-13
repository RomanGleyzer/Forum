using System.Text.Json;

namespace SocialNetworkAPI.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var status = exception is InvalidOperationException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        var message = status == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;

        var payload = new
        {
            status,
            message,
            traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
        };

        return context.Response.WriteAsJsonAsync(payload, JsonOptions, context.RequestAborted);
    }
}
