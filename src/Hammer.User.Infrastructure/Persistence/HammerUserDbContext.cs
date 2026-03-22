using Hammer.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hammer.User.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the Hammer User service.
/// </summary>
public sealed class HammerUserDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HammerUserDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public HammerUserDbContext(DbContextOptions<HammerUserDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the users.
    /// </summary>
    public DbSet<Domain.Entities.User> Users => Set<Domain.Entities.User>();

    /// <summary>
    /// Gets the OAuth accounts.
    /// </summary>
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();

    /// <summary>
    /// Gets the user devices.
    /// </summary>
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    /// <summary>
    /// Gets the refresh tokens.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HammerUserDbContext).Assembly);
    }
}
