using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace ecommerce.Core.Helpers;

public static class TypeHelper
{
    private static readonly HashSet<Type> FloatingTypes = new()
    {
        typeof(float),
        typeof(double),
        typeof(decimal)
    };

    private static readonly HashSet<Type> NonNullablePrimitiveTypes = new()
    {
        typeof(byte),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(sbyte),
        typeof(ushort),
        typeof(uint),
        typeof(ulong),
        typeof(bool),
        typeof(float),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid)
    };

    public static bool IsNonNullablePrimitiveType(Type type)
    {
        return NonNullablePrimitiveTypes.Contains(type);
    }

    public static bool IsFunc(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        var type = obj.GetType();
        if (!type.GetTypeInfo().IsGenericType)
        {
            return false;
        }

        return type.GetGenericTypeDefinition() == typeof(Func<>);
    }

    public static bool IsFunc<TReturn>(object? obj)
    {
        return obj is Func<TReturn>;
    }

    public static bool IsPrimitiveExtended(Type type, bool includeNullables = true, bool includeEnums = false)
    {
        if (IsPrimitiveExtendedInternal(type, includeEnums))
        {
            return true;
        }

        if (includeNullables && IsNullable(type) && type.GenericTypeArguments.Any())
        {
            return IsPrimitiveExtendedInternal(type.GenericTypeArguments[0], includeEnums);
        }

        return false;
    }

    public static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static Type GetFirstGenericArgumentIfNullable(this Type t)
    {
        if (t.GetGenericArguments().Length > 0 && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return t.GetGenericArguments().First();
        }

        return t;
    }

    public static bool IsEnumerable(Type type, [NotNullWhen(true)] out Type? itemType, bool includePrimitives = true)
    {
        if (!includePrimitives && IsPrimitiveExtended(type))
        {
            itemType = null;
            return false;
        }

        var enumerableTypes = ReflectionHelper.GetImplementedGenericTypes(type, typeof(IEnumerable<>));
        if (enumerableTypes.Count == 1)
        {
            itemType = enumerableTypes[0].GenericTypeArguments[0];
            return true;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            itemType = typeof(object);
            return true;
        }

        itemType = null;
        return false;
    }

    public static bool IsDictionary(Type type, out Type? keyType, out Type? valueType)
    {
        var dictionaryTypes = ReflectionHelper
            .GetImplementedGenericTypes(
                type,
                typeof(IDictionary<,>)
            );

        if (dictionaryTypes.Count == 1)
        {
            keyType = dictionaryTypes[0].GenericTypeArguments[0];
            valueType = dictionaryTypes[0].GenericTypeArguments[1];
            return true;
        }

        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            keyType = typeof(object);
            valueType = typeof(object);
            return true;
        }

        keyType = null;
        valueType = null;

        return false;
    }

    private static bool IsPrimitiveExtendedInternal(Type type, bool includeEnums)
    {
        if (type.IsPrimitive)
        {
            return true;
        }

        if (includeEnums && type.IsEnum)
        {
            return true;
        }

        return type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid);
    }

    public static T? GetDefaultValue<T>()
    {
        return default;
    }

    public static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }

    public static string GetFullNameHandlingNullableAndGenerics(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return type.GenericTypeArguments[0].FullName + "?";
        }

        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            return $"{genericType.FullName?[..genericType.FullName.IndexOf('`')]}<{type.GenericTypeArguments.Select(GetFullNameHandlingNullableAndGenerics).Aggregate((a, b) => a + "," + b)}>";
        }

        return type.FullName ?? type.Name;
    }

    public static string GetSimplifiedName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return GetSimplifiedName(type.GenericTypeArguments[0]) + "?";
        }

        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            return $"{genericType.FullName?[..genericType.FullName.IndexOf('`')]}<{type.GenericTypeArguments.Select(GetSimplifiedName).Aggregate((a, b) => a + "," + b)}>";
        }

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "boolean",
            TypeCode.Byte => "number",
            TypeCode.Char => "string",
            TypeCode.DateTime => "string",
            TypeCode.DBNull => "object",
            TypeCode.Decimal => "number",
            TypeCode.Double => "number",
            TypeCode.Empty => "object",
            TypeCode.Int16 => "number",
            TypeCode.Int32 => "number",
            TypeCode.Int64 => "number",
            TypeCode.Object => "object",
            TypeCode.SByte => "number",
            TypeCode.Single => "number",
            TypeCode.String => "string",
            TypeCode.UInt16 => "number",
            TypeCode.UInt32 => "number",
            TypeCode.UInt64 => "number",
            _ => type.FullName ?? type.Name
        };
    }

    public static object? ConvertFromString<TTargetType>(string value)
    {
        return ConvertFromString(typeof(TTargetType), value);
    }

    public static object? ConvertFromString(Type targetType, string? value)
    {
        if (value == null)
        {
            return null;
        }

        var converter = TypeDescriptor.GetConverter(targetType);

        if (IsFloatingType(targetType))
        {
            using (CultureHelper.Use(CultureInfo.InvariantCulture))
            {
                return converter.ConvertFromString(value.Replace(',', '.'));
            }
        }

        return converter.ConvertFromString(value);
    }

    public static bool IsFloatingType(Type type, bool includeNullable = true)
    {
        if (FloatingTypes.Contains(type))
        {
            return true;
        }

        return includeNullable &&
               IsNullable(type) &&
               FloatingTypes.Contains(type.GenericTypeArguments[0]);
    }

    public static bool IsNumericType(Type type, bool includeNullable = true)
    {
        var actualType = includeNullable ? StripNullable(type) : type;
        return actualType == typeof(int) ||
               actualType == typeof(long) ||
               actualType == typeof(short) ||
               actualType == typeof(byte) ||
               actualType == typeof(uint) ||
               actualType == typeof(ulong) ||
               actualType == typeof(ushort) ||
               actualType == typeof(sbyte) ||
               actualType == typeof(float) ||
               actualType == typeof(double) ||
               actualType == typeof(decimal);
    }

    public static object? ConvertFrom<TTargetType>(object value)
    {
        return ConvertFrom(typeof(TTargetType), value);
    }

    public static object? ConvertFrom(Type targetType, object value)
    {
        return TypeDescriptor
            .GetConverter(targetType)
            .ConvertFrom(value);
    }

    public static Type StripNullable(Type type)
    {
        return IsNullable(type)
            ? type.GenericTypeArguments[0]
            : type;
    }

    public static bool IsDefaultValue(object? obj)
    {
        return obj == null || obj.Equals(GetDefaultValue(obj.GetType()));
    }

    public static object? ChangeType(object? value, Type destinationType)
    {
        return ChangeType(value, destinationType, CultureInfo.InvariantCulture);
    }

    public static object? ChangeType(object? value, Type destinationType, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        var sourceType = value.GetType();

        var destinationConverter = TypeDescriptor.GetConverter(destinationType);
        if (destinationConverter.CanConvertFrom(value.GetType()))
        {
            return destinationConverter.ConvertFrom(null, culture, value);
        }

        var sourceConverter = TypeDescriptor.GetConverter(sourceType);
        if (sourceConverter.CanConvertTo(destinationType))
        {
            return sourceConverter.ConvertTo(null, culture, value, destinationType);
        }

        if (destinationType.IsEnum && value is int intValue)
        {
            return Enum.ToObject(destinationType, intValue);
        }

        if (!destinationType.IsInstanceOfType(value))
        {
            return Convert.ChangeType(value, destinationType, culture);
        }

        return value;
    }
}