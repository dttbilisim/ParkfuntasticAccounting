using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace ecommerce.Domain.Shared.Emailing;

public class EmailTokenDictionary : Dictionary<string, object?>
{
    public EmailTokenDictionary() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public EmailTokenDictionary(IDictionary<string, object?> dictionary) : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {
    }

    public EmailTokenDictionary(IEnumerable<KeyValuePair<string, object?>> collection) : base(collection, StringComparer.OrdinalIgnoreCase)
    {
    }

    public object? SetValue(string name, object? value, Type type)
    {
        return this[name] = ChangeType(value, type);
    }

    public TValue? SetValue<TValue>(string name, TValue? value)
    {
        return (TValue?) SetValue(name, value, typeof(TValue));
    }

    public EmailTokenDictionary SetObject(string name, EmailTokenDictionary value)
    {
        return (EmailTokenDictionary) (this[name] = value);
    }

    public EmailTokenDictionary[] SetArray(string name, EmailTokenDictionary[] value)
    {
        return (EmailTokenDictionary[]) (this[name] = value);
    }

    public List<EmailTokenDictionary> SetList(string name, List<EmailTokenDictionary> value)
    {
        return (List<EmailTokenDictionary>) (this[name] = value);
    }

    public object? GetByPathOrDefault(string path, object? defaultValue = null)
    {
        var paths = path.Split('.');
        object? value = null;

        foreach (var name in paths)
        {
            if (value is EmailTokenDictionary obj)
            {
                value = obj.GetValueOrDefault(name);
            }
            else if (value is EmailTokenDictionary[] arr)
            {
                value = arr.Select(childObj => childObj.GetValueOrDefault(name)).ToArray();
            }
            else
            {
                value = GetValueOrDefault(name);
            }

            if (value == null)
                return defaultValue;
        }

        return value;
    }

    public EmailTokenDictionary ConvertToNestedDictionary()
    {
        var result = new EmailTokenDictionary();

        foreach (var (key, value) in this)
        {
            var paths = key.Split('.');
            var current = result;

            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                var isLast = i == paths.Length - 1;

                if (isLast)
                {
                    if (value is JArray jArray)
                    {
                        var array = jArray.ToObject<List<EmailTokenDictionary>>() ?? new List<EmailTokenDictionary>();
                        current.SetList(path, array.Select(x => x.ConvertToNestedDictionary()).ToList());
                    }
                    else if (value is JObject jObject)
                    {
                        var obj = jObject.ToObject<EmailTokenDictionary>() ?? new EmailTokenDictionary();
                        current.SetObject(path, obj.ConvertToNestedDictionary());
                    }
                    else
                    {
                        current.SetValue(path, value);
                    }
                }
                else
                {
                    var child = current.GetObjectOrNull(path);

                    if (child == null)
                    {
                        child = new EmailTokenDictionary();
                        current.SetObject(path, child);
                    }

                    current = child;
                }
            }
        }

        return result;
    }

    public TValue? GetByPathOrDefault<TValue>(string path, TValue? defaultValue = default)
    {
        var value = GetByPathOrDefault(path);

        return ChangeTypeAs(value, defaultValue);
    }

    public object GetValue(string name)
    {
        return GetValueOrDefault(name)
               ?? throw new Exception($"Could not find a field with the given name: {name}");
    }

    public TValue GetValue<TValue>(string name)
    {
        return GetValueOrDefault<TValue>(name)
               ?? throw new Exception($"Could not find a field with the given name: {name}");
    }

    public object? GetValueOrDefault(string name, object? defaultValue = null)
    {
        return TryGetValue(name, out var value) ? value : defaultValue;
    }

    public TValue? GetValueOrDefault<TValue>(string name, TValue? defaultValue = default)
    {
        var value = GetValueOrDefault(name);

        return ChangeTypeAs(value, defaultValue);
    }

    public EmailTokenDictionary GetObject(string name)
    {
        return GetObjectOrNull(name)
               ?? throw new Exception($"Could not find a field with the given name: {name}");
    }

    public EmailTokenDictionary? GetObjectOrNull(string name)
    {
        return GetValueOrDefault(name) is EmailTokenDictionary val ? val : null;
    }

    public EmailTokenDictionary GetObjectOrAdd(string name, Func<string, EmailTokenDictionary> factory)
    {
        return GetValueOrDefault(name) is EmailTokenDictionary val
            ? val
            : SetObject(name, factory(name));
    }

    public EmailTokenDictionary[] GetArray(string name)
    {
        return GetArrayOrNull(name)
               ?? throw new Exception($"Could not find a field with the given name: {name}");
    }

    public EmailTokenDictionary[]? GetArrayOrNull(string name)
    {
        return GetValueOrDefault(name) is EmailTokenDictionary[] val ? val : null;
    }

    public EmailTokenDictionary[] GetArrayOrAdd(string name, Func<string, EmailTokenDictionary[]> factory)
    {
        return GetValueOrDefault(name) is EmailTokenDictionary[] val
            ? val
            : SetArray(name, factory(name));
    }

    public List<EmailTokenDictionary> GetList(string name)
    {
        return GetListOrNull(name)
               ?? throw new Exception($"Could not find a field with the given name: {name}");
    }

    public List<EmailTokenDictionary>? GetListOrNull(string name)
    {
        return GetValueOrDefault(name) is List<EmailTokenDictionary> val ? val : null;
    }

    public List<EmailTokenDictionary> GetListOrAdd(string name, Func<string, List<EmailTokenDictionary>> factory)
    {
        return GetValueOrDefault(name) is List<EmailTokenDictionary> val
            ? val.ToList()
            : SetList(name, factory(name));
    }

    public static object? ChangeType(object? value, Type type, object? defaultValue = null, CultureInfo? culture = null)
    {
        if (value == null)
        {
            return null;
        }

        culture ??= CultureInfo.CurrentCulture;

        var sourceType = value.GetType();

        var destinationConverter = TypeDescriptor.GetConverter(type);
        if (destinationConverter.CanConvertFrom(value.GetType()))
        {
            return destinationConverter.ConvertFrom(null, culture, value);
        }

        var sourceConverter = TypeDescriptor.GetConverter(sourceType);
        if (sourceConverter.CanConvertTo(type))
        {
            return sourceConverter.ConvertTo(null, culture, value, type);
        }

        if (type.IsEnum && value is int intValue)
        {
            return Enum.ToObject(type, intValue);
        }

        if (!type.IsInstanceOfType(value))
        {
            return Convert.ChangeType(value, type, culture);
        }

        return value;
    }

    public static TValue? ChangeTypeAs<TValue>(object? value, TValue? defaultValue = default)
    {
        return (TValue?) ChangeType(value, typeof(TValue), defaultValue);
    }
}