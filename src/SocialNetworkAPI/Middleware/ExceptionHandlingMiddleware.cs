using FluentValidation;
using System.Net;
using System.Text.Json;

namespace SocialNetworkAPI.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
   
    private readonly RequestDelegate _next = next 
        ?? throw new ArgumentNullException(nameof(next));

    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger 
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request?.Method, context.Request?.Path);
            await WriteProblemAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(HttpContext ctx, Exception ex)
    {
        var (status, title) = Map(ex);

        var problem = new
        {
            type = "about:blank",
            title,
            status,
            traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.TraceIdentifier
        };

        ctx.Response.Clear();
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, Json, ctx.RequestAborted);
    }

    private static (int Status, string Title) Map(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized"),
            ValidationException => ((int)HttpStatusCode.BadRequest, "Validation failed"),
            _ => ((int)HttpStatusCode.InternalServerError, "Unexpected error")
        };
    }
}
