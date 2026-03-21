using Hammer.User.Application.UseCases.Login;
using Hammer.User.Application.UseCases.Register;
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
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();

        return services;
    }
}
