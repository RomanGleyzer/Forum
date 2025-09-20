using Application.Features.Posts.Commands;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace SocialNetworkAPI.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var serviceName = entryAssembly?.GetName().Name ?? "SocialNetworkAPI";
        var serviceVersion =
            entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? entryAssembly?.GetName().Version?.ToString();

        var appAssembly = typeof(CreatePostCommand).Assembly;

        services.AddAutoMapper(cfg => { }, appAssembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
        services.AddValidatorsFromAssembly(appAssembly);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName, serviceVersion))
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("Application.Handlers");

                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase))
                    tracing.AddConsoleExporter();

                var otlp = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
                if (!string.IsNullOrWhiteSpace(otlp))
                    tracing.AddOtlpExporter();
            });

        return services;
    }
}
