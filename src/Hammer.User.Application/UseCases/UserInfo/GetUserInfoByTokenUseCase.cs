using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.UserInfo;

internal sealed class GetUserInfoByTokenUseCase(IUserRepository userRepository) : IGetUserInfoByTokenUseCase
{
    public async Task<UserInfoDetailResponse> ExecuteAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"유저를 찾을 수 없어요: {userId}");

        return UserInfoDetailResponse.FromEntity(user);
    }
}
