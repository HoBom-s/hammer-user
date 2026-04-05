using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.UserInfo;

internal sealed class DeleteUserInfoByIdUseCase(IUserRepository repository) : IDeleteUserInfoByIdUseCase
{
    public async Task<DeleteUserResponse> ExecuteAsync(Guid userId, CancellationToken ct)
    {
        var user = await repository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"유저를 찾을 수 없어요: {userId}");

        if (user.IsDeleted)
            throw new BadRequestException($"이미 삭제된 유저에요: {userId}");

        user.SoftDelete();
        await repository.SaveChangesAsync(ct);

        return DeleteUserResponse.FromEntity(user);
    }
}
