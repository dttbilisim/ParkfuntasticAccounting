using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace ecommerce.Core.Extensions;

public static class MemoryCacheExtensions
{
    public static void RemoveByPrefix(this IMemoryCache cache, string prefix)
    {
        RemoveByPattern(cache, $"^{prefix}.*");
    }

    public static void RemoveByPattern(this IMemoryCache cache, string pattern)
    {
        var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);

        if (field?.GetValue(cache) is not ICollection<KeyValuePair<object, object>> collection)
        {
            return;
        }

        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var matchesKeys = collection.Where(p => regex.IsMatch(p.Key.ToString() ?? string.Empty)).Select(p => p.Key).ToList();

        foreach (var key in matchesKeys)
        {
            cache.Remove(key);
        }
    }
}