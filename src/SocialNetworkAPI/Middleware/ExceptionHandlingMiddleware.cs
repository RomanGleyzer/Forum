using Application.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace SocialNetworkAPI.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
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

            var _ when ex.GetType().IsGenericType
                        && ex.GetType().GetGenericTypeDefinition() == typeof(NotFoundException<>)
                => ((int)HttpStatusCode.NotFound, "Not Found"),

            _ => ((int)HttpStatusCode.InternalServerError, "Unexpected error")
        };
    }
}
