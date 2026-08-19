using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Infrastructure.Logging;

public class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity == null) return;
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ActivityStartTime",
            activity.StartTimeUtc.ToString("o")));
    }
}