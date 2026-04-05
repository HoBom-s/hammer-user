using Hammer.User.Application.Common;

namespace Hammer.User.Application.UseCases.UserInfo;

public interface IDeleteUserInfoByIdUseCase
{
    public Task<DeleteUserResponse> ExecuteAsync(Guid userId, CancellationToken ct);
}
