using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.UserInfo;

internal sealed class GetUserInfoByIdUseCase(IUserRepository userRepository) : IGetUserInfoByIdUseCase
{
    public async Task<UserSummaryResponse> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"유저를 찾을 수 없어요: {id}");

        return UserSummaryResponse.FromEntity(user);
    }
}
