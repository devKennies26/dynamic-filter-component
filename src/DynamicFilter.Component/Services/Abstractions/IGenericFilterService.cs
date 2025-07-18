using Microsoft.EntityFrameworkCore;

namespace DynamicFilter.Component.Services.Abstractions;

public interface IGenericFilterService<TContext> where TContext : DbContext
{
    Task<List<object>> FilterEntitiesAsync(string filter, string entityName);
}