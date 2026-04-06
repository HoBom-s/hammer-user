using Hammer.User.Domain.Ports;
using Hammer.User.Infrastructure.OAuth;
using Hammer.User.Infrastructure.Persistence;
using Hammer.User.Infrastructure.Persistence.Repositories;
using Hammer.User.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.User.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services (EF Core, PostgreSQL, OAuth).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<HammerUserDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOAuthAccountRepository, OAuthAccountRepository>();
        services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ILegalDocumentRepository, LegalDocumentRepository>();

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        services
            .AddOptions<OAuthSettings>()
            .Bind(configuration.GetSection("OAuth"));

        services.AddHttpClient();

        services.AddSingleton<IOAuthProviderClient, GoogleOAuthClient>();
        services.AddSingleton<IOAuthProviderClient, AppleOAuthClient>();
        services.AddSingleton<IOAuthProviderClient, KakaoOAuthClient>();
        services.AddSingleton<IOAuthProviderClient, NaverOAuthClient>();
        services.AddSingleton<IOAuthUserInfoProvider, OAuthUserInfoProvider>();

        services.AddHostedService<DeletedUserCleanupService>();

        services
            .AddHealthChecks()
            .AddDbContextCheck<HammerUserDbContext>();

        return services;
    }
}
