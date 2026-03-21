using FluentAssertions;
using Hammer.User.Infrastructure.Services;

namespace Hammer.User.Tests.Infrastructure.Services;

public sealed class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var hash = _sut.Hash("Test1234!");

        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHashesForSamePassword()
    {
        var hash1 = _sut.Hash("Test1234!");
        var hash2 = _sut.Hash("Test1234!");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatches()
    {
        var password = "Test1234!";
        var hash = _sut.Hash(password);

        _sut.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        var hash = _sut.Hash("Test1234!");

        _sut.Verify("WrongPassword1!", hash).Should().BeFalse();
    }
}
