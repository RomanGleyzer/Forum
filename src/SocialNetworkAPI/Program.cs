using Application.Abstractions;
using Infrastructure.Extensions;
using Infrastructure.Logging;
using Infrastructure.Persistence.Context;
using Serilog;
using SocialNetworkAPI.Extensions;
using SocialNetworkAPI.Middleware;
using SocialNetworkAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.With<ActivityEnricher>();
});

builder.Services.AddApplicationServices();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IUserAvatarUrlProvider, UserAvatarUrlProvider>();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

builder.Services.AddControllers();

builder.Services.AddHealthChecks().AddDbContextCheck<SocialNetworkDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (ctx, _, __) =>
        ctx.Request.Path.StartsWithSegments("/health") ||
        ctx.Request.Path.StartsWithSegments("/favicon.ico") ||
        ctx.Request.Path.StartsWithSegments("/assets")
            ? Serilog.Events.LogEventLevel.Debug
            : Serilog.Events.LogEventLevel.Information;
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
