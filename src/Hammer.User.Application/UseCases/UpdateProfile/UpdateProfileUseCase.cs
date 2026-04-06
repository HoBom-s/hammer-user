using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.UpdateProfile;

/// <summary>
///     Updates the authenticated user's profile (nickname and/or password).
/// </summary>
internal sealed class UpdateProfileUseCase(IUserRepository repository, IPasswordHasher passwordHasher)
    : IUpdateProfileUseCase
{
    /// <inheritdoc />
    public async Task<UserSummaryResponse> ExecuteAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await repository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"유저를 찾을 수 없어요: {userId}");

        if (user.IsDeleted)
            throw new BadRequestException($"삭제된 유저에요: {userId}");

        if (request.Nickname is not null)
            user.UpdateNickname(request.Nickname);

        if (request.NewPassword is not null)
        {
            if (user.HasPassword())
            {
                if (request.CurrentPassword is null)
                    throw new BadRequestException("현재 비밀번호를 입력해주세요.");

                if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash!))
                    throw new BadRequestException("현재 비밀번호가 일치하지 않아요.");
            }

            user.SetPasswordHash(passwordHasher.Hash(request.NewPassword));
        }

        await repository.SaveChangesAsync(ct);

        return UserSummaryResponse.FromEntity(user);
    }
}
