using Hammer.User.Application.Common;

namespace Hammer.User.Application.UseCases.GetUsers;

public interface IGetUsersUseCase
{
    public Task<PagedResponse<UserSummaryResponse>> ExecuteAsync(GetUsersRequest request, CancellationToken ct);
}
