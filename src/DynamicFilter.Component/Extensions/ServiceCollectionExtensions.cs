using DynamicFilter.Component.Services;
using DynamicFilter.Component.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicFilter.Component.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFilterComponent(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericFilterService<>), typeof(GenericFilterService<>));

        return services;
    }
}