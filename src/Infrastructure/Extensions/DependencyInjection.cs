using System.Security.Claims;
using System.Text;
using Application.Abstractions;
using Application.Abstractions.Auth;
using Application.Abstractions.Identity;
using Application.Behaviors;
using Application.Common.Options;
using Application.Options;
using Domain.Entities;
using Infrastructure.Auth;
using Infrastructure.Identity;
using Infrastructure.Options;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Storage;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Infrastructure.Extensions;

public static class DependencyInjection
{
    private const string CorsDefaultPolicy = "Default";

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors:AllowedOrigins must be configured and non-empty.");

        services.AddCors(options =>
        {
            options.AddPolicy(CorsDefaultPolicy, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        var conString = config.GetConnectionString("PostgreSQLConnection")
                        ?? throw new InvalidOperationException("PostgreSQLConnection string is missing.");

        services.AddDbContext<SocialNetworkDbContext>(options =>
            options.UseNpgsql(conString, npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                config.GetSection("IdentityOptions:Password").Bind(options.Password);
                config.GetSection("IdentityOptions:User").Bind(options.User);
            })
            .AddEntityFrameworkStores<SocialNetworkDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPostReadModelRepository, PostReadModelRepository>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton<IAvatarStorage, LocalAvatarStorage>();

        var redisConnectionString = config.GetConnectionString("Redis")
                                    ?? throw new InvalidOperationException("Redis connection string is missing.");
        
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opts = ConfigurationOptions.Parse(redisConnectionString);
            opts.AbortOnConnectFail = false;
            opts.ConnectRetry = 3;
            opts.SyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(opts);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentUserCacheFactory, CurrentUserCacheFactory>();

        services.AddOptions<AvatarRulesOptions>()
            .BindConfiguration(AvatarRulesOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                static o => o.AllowedMimeTypes.All(m => m.StartsWith("image/", StringComparison.OrdinalIgnoreCase)),
                "All AllowedMimeTypes must start with 'image/'.")
            .ValidateOnStart();

        services.AddOptions<MediaStorageOptions>()
            .BindConfiguration(MediaStorageOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(static o => !Path.IsPathRooted(o.AvatarsPath),
                "AvatarsPath must be a relative path (not rooted).")
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .Validate(static o =>
                    !string.IsNullOrWhiteSpace(o.Key) &&
                    !string.IsNullOrWhiteSpace(o.Issuer) &&
                    !string.IsNullOrWhiteSpace(o.Audience),
                "JWT options must contain non-empty Key, Issuer and Audience.")
            .Validate(static o => Encoding.UTF8.GetBytes(o.Key).Length >= 32,
                "Jwt: Key must be at least 32 bytes.")
            .Validate(static o => o.ExpiresInMinutes >= 60,
                "Jwt: ExpiresInMinutes must be >= 60 to match API contract.")
            .ValidateOnStart();

        services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException("JWT configuration section is missing or empty.");

        var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Key);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.Zero,
                    ValidTypes = ["JWT"],
                    RoleClaimType = "role",
                    NameClaimType = ClaimTypes.NameIdentifier
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JWT")
                            .LogError(ctx.Exception, "JWT auth failed");
                        return Task.CompletedTask;
                    },
                    OnChallenge = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JWT");
                        logger.LogWarning("JWT challenge: {Error} {Description}", ctx.Error, ctx.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}