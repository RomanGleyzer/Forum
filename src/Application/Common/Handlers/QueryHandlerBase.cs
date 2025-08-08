using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Common.Handlers;

public abstract class QueryHandlerBase<TRequest, TResponse>(ILogger logger)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    protected readonly ILogger _logger = logger;

    public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);

    protected async Task<TResponse> ExecuteAsync(
        string activityName,
        TRequest request,
        Func<Activity?, Task<TResponse>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        using var activity = new ActivitySource(GetType().Name).StartActivity(activityName, ActivityKind.Server);

        SetTracingTags(activity, request);

        try
        {
            var response = await action(activity);
            LogSuccess(response, activity, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity, request);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("operation.end_time", DateTimeOffset.UtcNow);
        }
    }

    protected virtual void SetTracingTags(Activity? activity, TRequest request)
    {
        var correlationId = Activity.Current?.GetTagItem("correlation.id")?.ToString();
        activity?.SetTag("correlation.id", correlationId ?? string.Empty);
        activity?.SetTag("request.type", typeof(TRequest).Name);
        activity?.SetTag("request.body", System.Text.Json.JsonSerializer.Serialize(request));
        activity?.SetTag("operation.start_time", DateTimeOffset.UtcNow);
    }

    protected virtual void LogSuccess(TResponse response, Activity? activity, long? durationMs = null)
    {
        _logger.LogInformation("Successfully handled {RequestType}.", typeof(TRequest).Name);
        activity?.SetTag("status", "success");
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.AddEvent(new ActivityEvent("Success",
            tags: new ActivityTagsCollection { { "response.type", typeof(TResponse).Name } }));
        if (durationMs != null)
            activity?.SetTag("operation.duration_ms", durationMs.Value);

        LogEntitySuccess(response, activity);
    }

    protected virtual void LogEntitySuccess(TResponse response, Activity? activity) { }

    protected void HandleException(Exception ex, Activity? activity, TRequest? request = default)
    {
        _logger.LogError(ex, "Error handling {RequestType}: {Message}", typeof(TRequest).Name, ex.Message);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace ?? "" },
                { "request.body", request != null ? System.Text.Json.JsonSerializer.Serialize(request) : string.Empty }
            }));
    }
}
