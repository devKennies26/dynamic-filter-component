using DynamicFilter.Component.Services;
using DynamicFilter.Component.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicFilter.Component.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dynamic filter service in the service collection.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddDynamicFilter(this IServiceCollection services)
    {
        services.AddScoped(typeof(IDynamicFilterService<>), typeof(DynamicFilterService<>));

        return services;
    }
}