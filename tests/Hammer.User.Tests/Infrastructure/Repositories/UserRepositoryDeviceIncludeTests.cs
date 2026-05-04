using FluentAssertions;
using Hammer.User.Domain.Enums;
using Hammer.User.Infrastructure.Persistence;
using Hammer.User.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hammer.User.Tests.Infrastructure.Repositories;

public sealed class UserRepositoryDeviceIncludeTests : IDisposable
{
    private readonly HammerUserDbContext _context;
    private readonly UserRepository _sut;

    public UserRepositoryDeviceIncludeTests()
    {
        var options = new DbContextOptionsBuilder<HammerUserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new HammerUserDbContext(options);
        _sut = new UserRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeDevice()
    {
        var user = Domain.Entities.User.CreateWithCredentials("a@b.com", "nick", "hash");
        user.RegisterDevice(DevicePlatform.Ios, "dev-1", "tok-1");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(user.Id);

        loaded.Should().NotBeNull();
        loaded!.Device.Should().NotBeNull();
        loaded.Device!.PushToken.Should().Be("tok-1");
    }

    [Fact]
    public async Task RegisterDevice_ShouldReplaceExistingDevice()
    {
        // Arrange: user with an existing device
        var user = Domain.Entities.User.CreateWithCredentials("a@b.com", "nick", "hash");
        user.RegisterDevice(DevicePlatform.Ios, "dev-1", "tok-1");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act: load (includes device), replace, save
        var loaded = await _sut.GetByIdAsync(user.Id);
        loaded!.RegisterDevice(DevicePlatform.Android, "dev-2", "tok-2");
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Assert: exactly one device with new data
        var devices = await _context.UserDevices.Where(d => d.UserId == user.Id).ToListAsync();
        devices.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(
                new { Platform = DevicePlatform.Android, DeviceIdentifier = "dev-2", PushToken = "tok-2" },
                o => o.ExcludingMissingMembers());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
