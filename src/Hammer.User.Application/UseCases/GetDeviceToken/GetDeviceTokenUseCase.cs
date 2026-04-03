using System.Diagnostics.CodeAnalysis;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.GetDeviceToken;

/// <summary>
/// Retrieves a user's device push token from the repository.
/// </summary>
[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated via DI")]
internal sealed class GetDeviceTokenUseCase(IUserDeviceRepository deviceRepository) : IGetDeviceTokenUseCase
{
    public async Task<string?> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var device = await deviceRepository.GetByUserIdAsync(userId, ct);
        return device?.PushToken;
    }
}
