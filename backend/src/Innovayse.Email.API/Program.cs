using Innovayse.Email.API.Filters;
using Innovayse.Email.API.Middleware;
using Innovayse.Email.Application;
using Innovayse.Email.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Forwarded headers — trust X-Forwarded-Proto/-For from the Nuxt BFF, which sits behind the real edge proxy.
// KnownNetworks/KnownProxies are cleared because the docker-compose network's proxy IP isn't static.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// Health checks
builder.Services.AddHealthChecks();

// Controllers + auth gate
builder.Services.AddControllers();
builder.Services.AddScoped<RequireActiveMailboxFilter>();

// Application + Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseCors();

// Mailbox session middleware — decrypts the mail_session cookie into MailboxSessionHolder
app.UseMiddleware<MailboxSessionMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
