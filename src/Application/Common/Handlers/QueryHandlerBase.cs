using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Common.Handlers;

public abstract class QueryHandlerBase<TRequest, TResponse>(ILogger logger)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ActivitySource ActivitySource = new("Application.Queries");
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    public abstract Task<TResponse> Handle(TRequest request, CancellationToken ct);

    protected async Task<TResponse> ExecuteAsync(
        string activityName,
        CancellationToken ct,
        Func<Activity?, CancellationToken, Task<TResponse>> action)
    {
        using var activity = StartActivity(activityName);

        try
        {
            var response = await action(activity, ct).ConfigureAwait(false);
            LogSuccess(response, activity);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Logger.LogWarning("Handling {RequestType} was canceled.", typeof(TRequest).Name);
            activity?.SetStatus(ActivityStatusCode.Unset, "canceled");
            throw;
        }
        catch (Exception ex)
        {
            HandleException(ex, activity);
            throw;
        }
    }

    protected virtual Activity? StartActivity(string name)
    {
        var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        activity?.SetTag("request.type", typeof(TRequest).Name);
        return activity;
    }

    protected virtual void LogSuccess(TResponse response, Activity? activity)
    {
        activity?.SetTag("result.type", typeof(TResponse).Name);
        if (response is null)
            return;

        switch (response)
        {
            case Array a:
                activity?.SetTag("result.count", a.Length);
                break;
            case System.Collections.ICollection c:
                activity?.SetTag("result.count", c.Count);
                break;
            default:
                LogEntitySuccess(response, activity);
                break;
        }
    }

    protected void HandleException(Exception ex, Activity? activity)
    {
        Logger.LogError(ex, "Error handling {RequestType}: {Message}", typeof(TRequest).Name, ex.Message);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message }
            }));
    }

    protected virtual void LogEntitySuccess(TResponse response, Activity? activity) { }
}
