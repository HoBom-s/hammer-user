using dotenv.net;
using Hammer.User.Api.Middleware;
using Hammer.User.Application;
using Hammer.User.Application.Common;
using Hammer.User.Infrastructure;
using Hammer.User.Infrastructure.Logging;
using Hammer.User.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "live";
var envFileName = $".env.{env}";
var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

while (dir is not null && !File.Exists(Path.Combine(dir.FullName, envFileName)))
    dir = dir.Parent;

if (dir is not null)
    DotEnv.Load(new DotEnvOptions(envFilePaths: [Path.Combine(dir.FullName, envFileName)]));
else
    await Console.Error.WriteLineAsync($"Warning: {envFileName} not found in any parent directory.");

var builder = WebApplication.CreateBuilder(args);

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? string.Empty;

builder.Services.AddSerilog(configuration =>
{
    configuration.ReadFrom.Configuration(builder.Configuration);

    if (!string.IsNullOrWhiteSpace(kafkaBootstrapServers))
        configuration.WriteToKafkaErrors(kafkaBootstrapServers);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(s => !string.IsNullOrWhiteSpace(s.SecretKey), "Jwt:SecretKey is required.")
    .Validate(s => s.SecretKey.Length >= 32, "Jwt:SecretKey must be at least 32 characters.")
    .ValidateOnStart();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HammerUserDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Cache-Control"] = "no-store";

    var path = context.Request.Path.Value ?? string.Empty;
    var isScalar = path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase);

    context.Response.Headers["Content-Security-Policy"] = isScalar
        ? "default-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net"
        : "default-src 'none'";

    await next();
});

app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.MapControllers();

await app.RunAsync();
