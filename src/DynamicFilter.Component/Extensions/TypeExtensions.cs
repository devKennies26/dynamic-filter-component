using DynamicFilter.Component.Exceptions;
using System.Collections;

namespace DynamicFilter.Component.Extensions;
public static class TypeExtensions
{
    public static bool IsCollectionType(this Type type)
    {
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    public static Type GetCollectionElementType(this Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType()!;

        if (collectionType.IsGenericType)
            return collectionType.GetGenericArguments()[0];

        throw new InvalidFilterException($"Cannot determine element type for collection {collectionType.Name}");
    }

    public static object ConvertValue(this Type targetType, string value)
    {
        try
        {
            if (targetType == typeof(string))
                return value;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, true);

            return Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
        }
        catch
        {
            throw new InvalidFilterException($"Cannot convert value '{value}' to type {targetType.Name}");
        }
    }
}