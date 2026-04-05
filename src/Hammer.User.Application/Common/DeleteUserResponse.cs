namespace Hammer.User.Application.Common;

/// <summary>
///     Delete user information response DTO.
/// </summary>
/// <param name="Id">Deleted user's id.</param>
public sealed record DeleteUserResponse(
    Guid Id)
{
    public static DeleteUserResponse FromEntity(Domain.Entities.User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new DeleteUserResponse(user.Id);
    }
}
