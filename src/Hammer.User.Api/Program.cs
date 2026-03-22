using dotenv.net;
using Hammer.User.Api.Middleware;
using Hammer.User.Application;
using Hammer.User.Application.Common;
using Hammer.User.Infrastructure;
using Hammer.User.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(s => !string.IsNullOrWhiteSpace(s.SecretKey), "Jwt:SecretKey is required.")
    .Validate(s => s.SecretKey.Length >= 32, "Jwt:SecretKey must be at least 32 characters.")
    .ValidateOnStart();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
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

app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

await app.RunAsync();
