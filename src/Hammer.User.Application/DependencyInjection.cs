using Hammer.User.Application.UseCases.Device;
using Hammer.User.Application.UseCases.GetUsers;
using Hammer.User.Application.UseCases.Login;
using Hammer.User.Application.UseCases.Logout;
using Hammer.User.Application.UseCases.OAuthLogin;
using Hammer.User.Application.UseCases.RefreshToken;
using Hammer.User.Application.UseCases.Register;
using Hammer.User.Application.UseCases.UserInfo;
using Hammer.User.Application.UseCases.ValidateToken;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.User.Application;

/// <summary>
/// Application layer dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application services (use cases).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetUsersUseCase, GetUsersUseCase>();
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();
        services.AddScoped<IOAuthLoginUseCase, OAuthLoginUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
        services.AddScoped<IRegisterDeviceUseCase, RegisterDeviceUseCase>();
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IGetUserInfoByIdUseCase, GetUserInfoByIdUseCase>();
        services.AddScoped<IGetUserInfoByTokenUseCase, GetUserInfoByTokenUseCase>();
        services.AddScoped<IValidateTokenUseCase, ValidateTokenUseCase>();

        return services;
    }
}
