namespace Hammer.User.Domain.Ports;

/// <summary>
/// Abstraction for password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    public string Hash(string password);

    public bool Verify(string password, string hash);
}
