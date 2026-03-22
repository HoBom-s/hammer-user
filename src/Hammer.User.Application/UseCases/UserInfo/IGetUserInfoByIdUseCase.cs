using Hammer.User.Application.Common;

namespace Hammer.User.Application.UseCases.UserInfo;

public interface IGetUserInfoByIdUseCase
{
    public Task<UserSummaryResponse> ExecuteAsync(Guid id, CancellationToken ct);
}
