using Application.Exceptions;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;

namespace SocialNetworkAPI.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ILogger<ExceptionHandlingMiddleware> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request?.Method, context.Request?.Path);

            await WriteProblemAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(HttpContext ctx, Exception ex)
    {
        var (status, title, detail) = MapEx(ex);

        var problem = new
        {
            type = "about:blank",
            title,
            status,
            detail,
            traceId = Activity.Current?.Id ?? ctx.TraceIdentifier
        };

        ctx.Response.Clear();
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, Json, ctx.RequestAborted);
    }

    private static (int Status, string Title, object? Detail) MapEx(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", null),

            ValidationException v => (StatusCodes.Status400BadRequest, "Validation failed",
                new { errors = v.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) }),

            ArgumentException a => (StatusCodes.Status400BadRequest, "Bad request",
                new { error = a.Message }),

            NotFoundException<object> => (StatusCodes.Status404NotFound, "Not Found", null),

            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", null)
        };
    }
}