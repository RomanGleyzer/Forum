using Application.Abstractions;
using Application.Abstractions.Auth;
using Application.Abstractions.Identity;
using Application.Behaviors;
using Application.Common.Options;
using Domain.Entities;
using Infrastructure.Auth;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

namespace Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var conString = config.GetConnectionString("PostgreSQLConnection")
            ?? throw new InvalidOperationException("PostgreSQLConnection string is missing.");

        services.AddDbContext<SocialNetworkDbContext>(options =>
            options.UseNpgsql(conString));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            config.GetSection("IdentityOptions:Password").Bind(options.Password);
            config.GetSection("IdentityOptions:User").Bind(options.User);
        })
        .AddEntityFrameworkStores<SocialNetworkDbContext>()
        .AddDefaultTokenProviders();

        services.AddAuthentication();
        services.AddAuthorization();

        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                policy.WithOrigins(allowedOrigins ?? [])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPostReadModelRepository, PostReadModelRepository>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        var redisConnectionString = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "app:";
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration section is missing or empty.");

        var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Key);

        services.AddAuthentication(options =>
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
                ValidTypes = ["JWT"]
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


        return services;
    }
}