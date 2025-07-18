using Microsoft.EntityFrameworkCore;

namespace DynamicFilter.Component.Services.Abstractions;

/// <summary>
/// Provides functionality to dynamically filter entities based on a specified filter expression and entity type.
/// </summary>
/// <typeparam name="TContext">The type of the database context, which must derive from <see cref="DbContext"/>.</typeparam>
public interface IDynamicFilterService<TContext> where TContext : DbContext
{
    /// <summary>
    /// Filters entities of a specified type based on the provided filter expression.
    /// </summary>
    /// <param name="filter">
    /// The filter expression as a string, which can include sorting and filtering criteria.
    /// </param>
    /// <param name="entityName">
    /// The name of the entity type to filter.
    /// </param>
    /// <returns>A task that represents the asynchronous operation, containing a list of filtered entities.</returns>
    Task<List<object>> FilterEntitiesAsync(string filter, string entityName);
}