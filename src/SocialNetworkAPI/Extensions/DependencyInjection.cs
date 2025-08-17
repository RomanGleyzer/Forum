using System.Reflection;
using Application.Features.Posts.Commands;
using FluentValidation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

        services.AddAutoMapper(appAssembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(appAssembly));
        services.AddValidatorsFromAssembly(appAssembly);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(
                        "GetCurrentUserProfileQueryHandler",
                        "GetCurrentUserQueryHandler",
                        "UpdateUserCommandHandler",
                        "CreatePostCommandHandler",
                        "GetPostByIdQueryHandler",
                        "GetPostsByCursorQueryHandler",
                        "GetUserPostsQueryHandler",
                        "RegisterUserCommandHandler",
                        "LoginUserCommandHandler")
                    .AddConsoleExporter(); // только в Development
            });

        return services;
    }
}
