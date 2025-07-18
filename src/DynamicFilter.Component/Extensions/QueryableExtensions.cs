using DynamicFilter.Component.Exceptions;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicFilter.Component.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, Dictionary<string, string> filterParts,
        PropertyInfo[] properties) where T : class
    {
        Dictionary<string, string> minFilters = filterParts
            .Where(kv => kv.Key.StartsWith("min") && kv.Key.Length > 3)
            .ToDictionary(kv => kv.Key[3..], kv => kv.Value);

        Dictionary<string, string> maxFilters = filterParts
            .Where(kv => kv.Key.StartsWith("max") && kv.Key.Length > 3)
            .ToDictionary(kv => kv.Key[3..], kv => kv.Value);

        foreach (KeyValuePair<string, string> kv in filterParts)
        {
            string key = kv.Key;
            string value = kv.Value;

            if (key == "sortby" || key == "sortdescending" || key.StartsWith("min") || key.StartsWith("max"))
                continue;

            if (key.Contains('.'))
            {
                query = query.ApplyNestedFilter(key, value);
                continue;
            }

            PropertyInfo? property =
                properties.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (property is null)
                throw new InvalidFilterException($"Field '{key}' does not exist");

            string propNameLower = property.Name.ToLower();
            bool hasMin = minFilters.ContainsKey(propNameLower);
            bool hasMax = maxFilters.ContainsKey(propNameLower);

            if (hasMin || hasMax)
                continue;

            query = query.ApplyPropertyFilter(property, value);
        }

        // Min və Max filterləri
        query = query.ApplyMinMaxFilters(minFilters, maxFilters, properties);

        return query;
    }

    public static IQueryable<T> ApplyNestedFilter<T>(this IQueryable<T> query, string propertyPath, string value) where T : class
    {
        try
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            Expression expression = parameter;
            Type currentType = typeof(T);
            string[] propertyNames = propertyPath.Split('.');

            for (int i = 0; i < propertyNames.Length; i++)
            {
                string propertyName = propertyNames[i];
                PropertyInfo property = currentType.GetProperty(propertyName,
                                            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                                        ?? throw new InvalidFilterException(
                                            $"Property '{propertyName}' not found on type '{currentType.Name}'");

                if (property.PropertyType.IsCollectionType() && i < propertyNames.Length - 1)
                {
                    Type elementType = property.PropertyType.GetCollectionElementType();
                    expression = Expression.Property(expression, property);

                    ParameterExpression collectionParam = Expression.Parameter(elementType, "y");
                    Expression nestedExpression = collectionParam;
                    Type nestedCurrentType = elementType;

                    for (int j = i + 1; j < propertyNames.Length; j++)
                    {
                        string nestedPropName = propertyNames[j];
                        PropertyInfo nestedProp = nestedCurrentType.GetProperty(nestedPropName,
                                                      BindingFlags.IgnoreCase | BindingFlags.Public |
                                                      BindingFlags.Instance)
                        ?? throw new InvalidFilterException($"Property '{nestedPropName}' not found on type '{nestedCurrentType.Name}'");

                        nestedExpression = Expression.Property(nestedExpression, nestedProp);
                        nestedCurrentType = nestedProp.PropertyType;
                    }

                    object convertedValue = nestedCurrentType.ConvertValue(value);
                    BinaryExpression equality = Expression.Equal(
                        nestedExpression,
                        Expression.Constant(convertedValue));

                    LambdaExpression anyLambda = Expression.Lambda(equality, collectionParam);

                    MethodInfo anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elementType);

                    MethodCallExpression anyCall = Expression.Call(anyMethod, expression, anyLambda);

                    return query.Where(Expression.Lambda<Func<T, bool>>(anyCall, parameter));
                }

                expression = Expression.Property(expression, property);
                currentType = property.PropertyType;
            }

            object finalValue = currentType.ConvertValue(value);
            BinaryExpression finalEquality = Expression.Equal(expression, Expression.Constant(finalValue));
            return query.Where(Expression.Lambda<Func<T, bool>>(finalEquality, parameter));
        }
        catch (InvalidFilterException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidFilterException($"Error applying filter '{propertyPath}={value}': {ex.Message}");
        }
    }

    public static IQueryable<T> ApplyPropertyFilter<T>(this IQueryable<T> query, PropertyInfo property, string value)
        where T : class
    {
        try
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            MemberExpression propertyAccess = Expression.Property(parameter, property);

            if (property.PropertyType == typeof(string))
            {
                MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                MethodCallExpression containsExpression =
                    Expression.Call(propertyAccess, containsMethod, Expression.Constant(value));
                Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(containsExpression, parameter);
                return query.Where(lambda);
            }
            else
            {
                object convertedValue = Convert.ChangeType(value, property.PropertyType);
                BinaryExpression equalExpression =
                    Expression.Equal(propertyAccess, Expression.Constant(convertedValue));
                Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(equalExpression, parameter);
                return query.Where(lambda);
            }
        }
        catch
        {
            throw new InvalidFilterException($"Invalid value for '{property.Name}'");
        }
    }

    public static IQueryable<T> ApplyMinMaxFilters<T>(this IQueryable<T> query,
        Dictionary<string, string> minFilters,
        Dictionary<string, string> maxFilters,
        PropertyInfo[] properties) where T : class
    {
        foreach (KeyValuePair<string, string> min in minFilters)
        {
            PropertyInfo? prop =
                properties.FirstOrDefault(p => p.Name.Equals(min.Key, StringComparison.OrdinalIgnoreCase));
            if (prop is null)
                throw new InvalidFilterException($"Field '{min.Key}' does not exist for min filter");

            object val = Convert.ChangeType(min.Value, prop.PropertyType);
            query = query.ApplyComparisonFilter(prop, val, "GreaterThanOrEqual");
        }

        foreach (KeyValuePair<string, string> max in maxFilters)
        {
            PropertyInfo? prop =
                properties.FirstOrDefault(p => p.Name.Equals(max.Key, StringComparison.OrdinalIgnoreCase));
            if (prop is null)
                throw new InvalidFilterException($"Field '{max.Key}' does not exist for max filter");

            object val = Convert.ChangeType(max.Value, prop.PropertyType);
            query = query.ApplyComparisonFilter(prop, val, "LessThanOrEqual");
        }

        return query;
    }

    public static IQueryable<T> ApplyComparisonFilter<T>(this IQueryable<T> query, PropertyInfo property, object value,
        string comparisonType) where T : class
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        MemberExpression propertyAccess = Expression.Property(parameter, property);

        Expression comparison;
        switch (comparisonType)
        {
            case "GreaterThanOrEqual":
                comparison = Expression.GreaterThanOrEqual(propertyAccess, Expression.Constant(value));
                break;
            case "LessThanOrEqual":
                comparison = Expression.LessThanOrEqual(propertyAccess, Expression.Constant(value));
                break;
            default:
                throw new InvalidOperationException($"Unknown comparison type: {comparisonType}");
        }

        Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
        return query.Where(lambda);
    }
}