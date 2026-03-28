using Hammer.User.Application.Common;

namespace Hammer.User.Application.UseCases.UserInfo;

public interface IGetUserInfoByTokenUseCase
{
    public Task<UserInfoDetailResponse> ExecuteAsync(Guid userId, CancellationToken ct);
}
