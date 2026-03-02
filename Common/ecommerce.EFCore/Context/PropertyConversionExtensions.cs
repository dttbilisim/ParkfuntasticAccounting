using System.Reflection;
using ecommerce.Core.Helpers;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace ecommerce.EFCore.Context;

public static class PropertyConversionExtensions
{
    public static PropertyBuilder<TProperty> HasJsonConversion<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        var isEnumerable = TypeHelper.IsEnumerable(typeof(TProperty), out _, false);

        return propertyBuilder.HasConversion(
            GetJsonValueConverter<TProperty>(isEnumerable),
            GetJsonValueComparer<TProperty>()
        );
    }

    public static ValueConverter<TProperty, string> GetJsonValueConverter<TProperty>(bool isEnumerable = false)
    {
        return new ValueConverter<TProperty, string>(
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<TProperty>(v) ?? (isEnumerable ? Activator.CreateInstance<TProperty>() : default!)
        );
    }

    public static ValueComparer<TProperty> GetJsonValueComparer<TProperty>()
    {
        return new ValueComparer<TProperty>(
            (c1, c2) => JsonConvert.SerializeObject(c1) == JsonConvert.SerializeObject(c2),
            c => JsonConvert.SerializeObject(c).GetHashCode(),
            c => JsonConvert.DeserializeObject<TProperty>(JsonConvert.SerializeObject(c))!
        );
    }

    public static ValueComparer<TProperty> GetJsonDefaultValueComparer<TProperty>()
    {
        return new ValueComparer<TProperty>(
            (c1, c2) => c1 != null && c2 != null && c1.Equals(c2),
            c => c != null ? c.GetHashCode() : 0,
            c => c
        );
    }

    public static ValueComparer<TProperty> GetListValueComparer<TProperty, TItem>() where TProperty : IEnumerable<TItem>
    {
        return new ValueComparer<TProperty>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            c => c
        );
    }

    private static ValueComparer<TProperty> GetListValueComparerFromType<TProperty>(Type itemType)
    {
        var comparer = typeof(PropertyConversionExtensions)
            .GetMethod(nameof(GetListValueComparer), BindingFlags.Static | BindingFlags.Public)!
            .MakeGenericMethod(typeof(TProperty), itemType)
            .Invoke(null, null);

        return (ValueComparer<TProperty>) comparer!;
    }
}