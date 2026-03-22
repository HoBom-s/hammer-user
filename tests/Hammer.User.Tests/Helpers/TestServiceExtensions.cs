using Microsoft.Extensions.DependencyInjection;

namespace Hammer.User.Tests.Helpers;

internal static class TestServiceExtensions
{
    internal static void ReplaceService<T>(this IServiceCollection services, T implementation)
        where T : class
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddScoped(_ => implementation);
    }
}
