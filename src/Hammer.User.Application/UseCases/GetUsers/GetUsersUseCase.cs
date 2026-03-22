using Hammer.User.Application.Common;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.GetUsers;

internal sealed class GetUsersUseCase(IUserRepository userRepository) : IGetUsersUseCase
{
    public async Task<PagedResponse<UserSummaryResponse>> ExecuteAsync(GetUsersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (items, totalCount) = await userRepository.GetPagedAsync(request.Page, request.Size, request.Status, ct);

        var responses = items.Select(UserSummaryResponse.FromEntity).ToList();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.Size);

        return new PagedResponse<UserSummaryResponse>(responses, request.Page, request.Size, totalCount, totalPages);
    }
}
