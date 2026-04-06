using Hammer.User.Application.Common;

namespace Hammer.User.Application.UseCases.UpdateProfile;

/// <summary>
///     Updates the authenticated user's profile (nickname and/or password).
/// </summary>
public interface IUpdateProfileUseCase
{
    /// <summary>
    ///     Executes the profile update.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated user summary.</returns>
    public Task<UserSummaryResponse> ExecuteAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct);
}
