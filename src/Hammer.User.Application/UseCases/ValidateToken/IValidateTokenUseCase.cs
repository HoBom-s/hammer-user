namespace Hammer.User.Application.UseCases.ValidateToken;

public interface IValidateTokenUseCase
{
    public Task<ValidateTokenResponse> ExecuteAsync(ValidateTokenRequest request, CancellationToken ct);
}
