using Application.Features.Posts.Commands;
using FluentValidation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SocialNetworkAPI.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(CreatePostCommand).Assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreatePostCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(CreatePostCommand).Assembly);

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SocialNetworkAPI"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("GetCurrentUserProfileQueryHandler")
                    .AddSource("GetCurrentUserQueryHandler")
                    .AddSource("UpdateUserCommandHandler")
                    .AddSource("CreatePostCommandHandler")
                    .AddSource("GetPostByIdQueryHandler")
                    .AddSource("GetPostsByCursorQueryHandler")
                    .AddSource("GetUserPostsQueryHandler")
                    .AddSource("RegisterUserCommandHandler")
                    .AddSource("LoginUserCommandHandler")
                    .AddConsoleExporter();
            });

        return services;
    }
}
