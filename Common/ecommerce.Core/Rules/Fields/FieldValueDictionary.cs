using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ecommerce.Core.Helpers;

namespace ecommerce.Core.Rules.Fields;

public class FieldValueDictionary : Dictionary<string, object?>
{
    private readonly IDictionary<string, FieldDefinition>? _fields;

    public FieldValueDictionary()
    {
    }

    public FieldValueDictionary(IDictionary<string, FieldDefinition> fields)
    {
        _fields = fields;
    }

    public FieldValueDictionary(IEnumerable<FieldDefinition> fields)
    {
        _fields = fields.ToDictionary(f => f.Name, f => f);
    }

    public IReadOnlyList<FieldDefinition>? GetFields()
    {
        return _fields?.Values.ToImmutableList();
    }

    public new object? this[string key]
    {
        get => base[key];
        set
        {
            if (ValidateKeyValue(key, value))
            {
                base[key] = value;
            }
        }
    }

    public new void Add(string key, object? value)
    {
        if (ValidateKeyValue(key, value))
        {
            base.Add(key, value);
        }
    }

    public void AddWithoutValidation(string key, object? value)
    {
        if (ValidateKeyValue(key, value, false))
        {
            base.Add(key, value);
        }
    }

    private bool ValidateKeyValue(string key, [NotNullWhen(true)] object? value, bool validateType = true)
    {
        if (CheckValueIsEmpty(value))
        {
            if (ContainsKey(key))
            {
                Remove(key);
            }

            return false;
        }

        if (_fields == null)
        {
            return true;
        }

        var field = _fields.TryGetValue(key, out var fieldDefinition) ? fieldDefinition : null;

        if (field == null)
        {
            throw new ArgumentException($"Could not find a field definition with the given name: {key}");
        }

        if (!validateType)
        {
            return true;
        }

        var valueType = TypeHelper.StripNullable(value.GetType());
        var fieldType = TypeHelper.StripNullable(field.Type);

        if (TypeHelper.IsEnumerable(fieldType, out var fieldItemType, false) && TypeHelper.IsEnumerable(valueType, out var valueItemType, false))
        {
            fieldType = fieldItemType;
            valueType = valueItemType;
        }

        if (fieldType != valueType)
        {
            throw new ArgumentException($"Value type not valid for {key} field definition: FieldType {field.Type}, ValueType {valueType}");
        }

        return true;
    }

    private bool CheckValueIsEmpty([NotNullWhen(false)] object? value)
    {
        if (value is string s)
        {
            return string.IsNullOrEmpty(s);
        }

        return value == null;
    }

    public object? SetValue(string name, object? value, Type type)
    {
        return this[name] = ChangeType(value, type)!;
    }

    public TValue? SetValue<TValue>(string name, TValue? value)
    {
        return (TValue?) SetValue(name, value, typeof(TValue));
    }

    public object? SetValueWithoutValidation(string name, object? value, Type type)
    {
        if (!ValidateKeyValue(name, value, false))
        {
            return null;
        }

        return base[name] = ChangeType(value, type);
    }

    public TValue? SetValueWithoutValidation<TValue>(string name, TValue? value)
    {
        return (TValue?) SetValueWithoutValidation(name, value, typeof(TValue));
    }

    public FieldValueDictionary SetObject(string name, FieldValueDictionary value)
    {
        return (FieldValueDictionary) (this[name] = value);
    }

    public FieldValueDictionary[] SetArray(string name, FieldValueDictionary[] value)
    {
        return (FieldValueDictionary[]) (this[name] = value);
    }

    public List<FieldValueDictionary> SetList(string name, List<FieldValueDictionary> value)
    {
        return (List<FieldValueDictionary>) (this[name] = value);
    }

    public object? GetByPathOrDefault(string path, object? defaultValue = null)
    {
        var paths = path.Split('.');
        object? value = null;

        foreach (var name in paths)
        {
            if (value is FieldValueDictionary obj)
            {
                value = obj.GetValueOrDefault(name);
            }
            else if (value is FieldValueDictionary[] arr)
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

    public TValue? GetByPathOrDefault<TValue>(string path, TValue? defaultValue = default)
    {
        var value = GetByPathOrDefault(path);

        return ChangeTypeAs(value, defaultValue);
    }

    public object GetValue(string name)
    {
        return GetValueOrDefault(name)
               ?? throw new ArgumentException($"Could not find a field with the given name: {name}");
    }

    public TValue GetValue<TValue>(string name)
    {
        return GetValueOrDefault<TValue>(name)
               ?? throw new ArgumentException($"Could not find a field with the given name: {name}");
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

    public FieldValueDictionary GetObject(string name)
    {
        return GetObjectOrNull(name)
               ?? throw new ArgumentException($"Could not find a field with the given name: {name}");
    }

    public FieldValueDictionary? GetObjectOrNull(string name)
    {
        return GetValueOrDefault(name) as FieldValueDictionary;
    }

    public FieldValueDictionary GetObjectOrAdd(string name, Func<string, FieldValueDictionary> factory)
    {
        return GetValueOrDefault(name) is FieldValueDictionary val
            ? val
            : SetObject(name, factory(name));
    }

    public FieldValueDictionary[] GetArray(string name)
    {
        return GetArrayOrNull(name)
               ?? throw new ArgumentException($"Could not find a field with the given name: {name}");
    }

    public FieldValueDictionary[]? GetArrayOrNull(string name)
    {
        return GetValueOrDefault(name) as FieldValueDictionary[];
    }

    public FieldValueDictionary[] GetArrayOrAdd(string name, Func<string, FieldValueDictionary[]> factory)
    {
        return GetValueOrDefault(name) as FieldValueDictionary[] ?? SetArray(name, factory(name));
    }

    public List<FieldValueDictionary> GetList(string name)
    {
        return GetListOrNull(name)
               ?? throw new ArgumentException($"Could not find a field with the given name: {name}");
    }

    public List<FieldValueDictionary>? GetListOrNull(string name)
    {
        return GetValueOrDefault(name) as List<FieldValueDictionary>;
    }

    public List<FieldValueDictionary> GetListOrAdd(string name, Func<string, List<FieldValueDictionary>> factory)
    {
        return GetValueOrDefault(name) is List<FieldValueDictionary> val
            ? val.ToList()
            : SetList(name, factory(name));
    }

    public static object? ChangeType(object? value, Type type, object? defaultValue = null)
    {
        if (value == null)
        {
            return defaultValue;
        }

        if (TypeHelper.IsPrimitiveExtended(type, includeEnums: true))
        {
            var conversionType = type;
            if (TypeHelper.IsNullable(conversionType))
            {
                conversionType = conversionType.GetFirstGenericArgumentIfNullable();
            }

            if (conversionType == typeof(Guid))
            {
                return TypeDescriptor.GetConverter(conversionType).ConvertFromInvariantString(value.ToString() ?? string.Empty);
            }

            if (conversionType.IsEnum)
            {
                return TypeDescriptor.GetConverter(conversionType).ConvertFromInvariantString(value.ToString() ?? string.Empty);
            }

            return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }

        throw new ArgumentException("ChangeType does not support non-primitive types. Use non-generic GetProperty method and handle type casting manually.");
    }

    public static TValue? ChangeTypeAs<TValue>(object? value, TValue? defaultValue = default)
    {
        return (TValue?) ChangeType(value, typeof(TValue), defaultValue);
    }

    public static object? ChangeTypeWithCurrentCulture(object? value, Type type, object? defaultValue = null)
    {
        if (value == null)
        {
            return defaultValue;
        }

        if (TypeHelper.IsPrimitiveExtended(type, includeEnums: true))
        {
            var conversionType = type;
            if (TypeHelper.IsNullable(conversionType))
            {
                conversionType = conversionType.GetFirstGenericArgumentIfNullable();
            }

            if (conversionType == typeof(Guid))
            {
                return TypeDescriptor.GetConverter(conversionType).ConvertFromInvariantString(value.ToString() ?? string.Empty);
            }

            if (conversionType.IsEnum)
            {
                return TypeDescriptor.GetConverter(conversionType).ConvertFromInvariantString(value.ToString() ?? string.Empty);
            }

            return Convert.ChangeType(value, conversionType, CultureInfo.CurrentCulture);
        }

        throw new ArgumentException("ChangeType does not support non-primitive types. Use non-generic GetProperty method and handle type casting manually.");
    }
}