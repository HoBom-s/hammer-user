using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.Register;

/// <summary>
///     Registers a new user with email and password.
/// </summary>
internal sealed class RegisterUserUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILegalDocumentRepository legalDocumentRepository)
    : IRegisterUserUseCase
{
    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken ct)
    {
        if (!request.AgreeToTerms)
            throw new BadRequestException("이용약관에 동의해야 합니다.");

        await CheckUserEmailAsync(request.Email, ct);

        var terms = await legalDocumentRepository.GetLatestByTypeAsync(LegalDocumentType.TermsOfService, ct)
            ?? throw new NotFoundException("이용약관을 찾을 수 없습니다.");

        var user = Domain.Entities.User.CreateWithCredentials(
            request.Email,
            request.Nickname,
            passwordHasher.Hash(request.Password),
            terms.Version);

        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);

        return new RegisterUserResponse(user.Id, user.Email!, user.Nickname);
    }

    private async Task CheckUserEmailAsync(string email, CancellationToken ct)
    {
        var existingUser = await userRepository.GetByEmailAsync(email, ct);

        if (existingUser is not null)
            throw new ConflictException("이미 존재하는 이메일입니다.");
    }
}
