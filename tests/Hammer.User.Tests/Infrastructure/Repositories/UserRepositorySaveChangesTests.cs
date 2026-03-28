using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Infrastructure.Persistence;
using Hammer.User.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Hammer.User.Tests.Infrastructure.Repositories;

public sealed class UserRepositorySaveChangesTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldThrowConflictException_WhenConcurrencyConflictOccurs()
    {
        using var context = CreateThrowingContext(new DbUpdateConcurrencyException());
        var sut = new UserRepository(context);

        var act = () => sut.SaveChangesAsync();

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrowConflictException_WhenUniqueViolationOccurs()
    {
        var postgresException = CreatePostgresException(PostgresErrorCodes.UniqueViolation);
        using var context = CreateThrowingContext(new DbUpdateException("unique violation", postgresException));
        var sut = new UserRepository(context);

        var act = () => sut.SaveChangesAsync();

        await act.Should().ThrowAsync<ConflictException>();
    }

    private static PostgresException CreatePostgresException(string sqlState)
    {
        var exception = (PostgresException)RuntimeHelpers.GetUninitializedObject(typeof(PostgresException));
        typeof(PostgresException)
            .GetField("<SqlState>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(exception, sqlState);
        return exception;
    }

    private static HammerUserDbContext CreateThrowingContext(Exception exception)
    {
        var options = new DbContextOptionsBuilder<HammerUserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new ThrowingSaveChangesInterceptor(exception))
            .Options;

        return new HammerUserDbContext(options);
    }

    private sealed class ThrowingSaveChangesInterceptor(Exception exception) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw exception;
    }
}
