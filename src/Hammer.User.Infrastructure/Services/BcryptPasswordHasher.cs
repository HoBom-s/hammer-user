using Hammer.User.Domain.Ports;

namespace Hammer.User.Infrastructure.Services;

/// <summary>
///     BCrypt-based password hasher.
/// </summary>
internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
