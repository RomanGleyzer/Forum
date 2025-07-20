using Application.Behaviors;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
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


        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConnectionString = config.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Redis connection string is missing.");
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost";
            options.InstanceName = "local";
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtConfig = config.GetSection("Jwt");
        var key = jwtConfig["Key"] ?? throw new ArgumentNullException(nameof(config), "Jwt:Key configuration is missing.");
        var issuer = jwtConfig["Issuer"];
        var audience = jwtConfig["Audience"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }
}