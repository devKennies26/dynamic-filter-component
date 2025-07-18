using DynamicFilter.Component.Exceptions;
using DynamicFilter.Component.Extensions;
using DynamicFilter.Component.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicFilter.Component.Services;

public class GenericFilterService<TContext> : IGenericFilterService<TContext> where TContext : DbContext
{
    private readonly TContext _context;

    public GenericFilterService(TContext context)
    {
        _context = context;
    }

    public async Task<List<object>> FilterEntitiesAsync(string filter, string entityName)
    {
        if (await _context.Database.CanConnectAsync())
        {
            Type? entityType = _context.Model.GetEntityTypes()
                .FirstOrDefault(et => et.ClrType.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase))?.ClrType;
            if (entityType is null)
                throw new EntityNotFoundException(entityName);

            MethodInfo method = typeof(GenericFilterService<TContext>)
                .GetMethod(nameof(BuildQuery), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType);

            List<object> result = await (Task<List<object>>)method.Invoke(this, new object[] { filter })!;
            return result;
        }

        throw new InvalidOperationException("Database connection is not available");
    }

    private async Task<List<object>> BuildQuery<T>(string filter) where T : class
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new InvalidFilterException("Filter string cannot be empty");

        IQueryable<T> query = _context.Set<T>().AsQueryable();
        PropertyInfo[] properties = typeof(T).GetProperties();

        Dictionary<string, string> filterParts = filter.Split(',')
            .Select(f => f.Split('='))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0].Trim().ToLower(),
                parts => parts[1].Trim());

        if (filterParts.TryGetValue("sortby", out var sortByValue))
            ApplySort(ref query, sortByValue, filterParts.ContainsKey("sortdescending") &&
                                             filterParts["sortdescending"].ToLower() == "true");

        query = query.ApplyFilters(filterParts, properties);

        return (await query.ToListAsync()).Cast<object>().ToList();
    }

    private static void ApplySort<T>(ref IQueryable<T> query, string sortByValue, bool descending) where T : class
    {
        string[] sortProperties = sortByValue.Split('.');
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression propertyAccess = parameter;
        Type currentType = typeof(T);

        foreach (string propName in sortProperties)
        {
            PropertyInfo? prop = currentType.GetProperty(propName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                throw new InvalidFilterException($"Sort field '{propName}' does not exist");

            propertyAccess = Expression.Property(propertyAccess, prop);
            currentType = prop.PropertyType; // Nested property üçün cari tipi yenilə
        }

        LambdaExpression lambda = Expression.Lambda(propertyAccess, parameter);
        string methodName = descending ? "OrderByDescending" : "OrderBy";

        MethodInfo orderByMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), propertyAccess.Type);

        query = (IQueryable<T>)orderByMethod.Invoke(null, new object[] { query, lambda })!;
    }
}