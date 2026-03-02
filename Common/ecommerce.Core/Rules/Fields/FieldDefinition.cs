using System.Collections.Immutable;
using System.Text;

namespace ecommerce.Core.Rules.Fields;

public class FieldDefinition
{
    public string Name { get; }

    public string DisplayName { get; }

    public bool IsEnabled { get; }

    public Type Type { get; }

    public Type ValueProviderType { get; }

    public string PropertyPath => GetPropertyPath();

    public FieldDefinition? Parent { get; private set; }

    public FieldDefinitionValueSelectList? SelectList { get; private set; }

    public IReadOnlyList<FieldDefinition> Children => _children.Values.ToImmutableList();
    private readonly Dictionary<string, FieldDefinition> _children;

    public static readonly Type FieldObjectType = typeof(FieldValueDictionary);
    public static readonly Type FieldArrayType = typeof(FieldValueDictionary[]);

    public FieldDefinition(
        string name,
        Type type,
        Type valueProviderType,
        string? displayName = null,
        bool isEnabled = true)
    {
        Name = name;
        Type = type;
        ValueProviderType = valueProviderType.IsAssignableTo(typeof(IFieldDefinitionValueProvider)) ? valueProviderType : throw new ArgumentException("Value provider type must be assignable to IFieldDefinitionValueProvider");
        DisplayName = displayName ?? name;
        IsEnabled = isEnabled;

        _children = new Dictionary<string, FieldDefinition>();
    }

    public FieldDefinition AddChild(
        string name,
        Type type,
        Type valueProviderType,
        string? displayName = null,
        bool isEnabled = true)
    {
        if (Type != FieldObjectType && Type != FieldArrayType)
        {
            throw new ArgumentException("Parent field must be object or array for adding children.");
        }

        var child = new FieldDefinition(
            name,
            type,
            valueProviderType,
            displayName,
            isEnabled
        )
        {
            Parent = this
        };

        if (_children.ContainsKey(child.Name))
        {
            throw new ArgumentException("Duplicate field name: " + child.Name);
        }

        _children.Add(child.Name, child);

        return child;
    }

    public FieldDefinition GetChild(string name)
    {
        var field = GetOrNullChild(name);

        if (field == null)
        {
            throw new ArgumentException("Undefined field: " + name);
        }

        return field;
    }

    public FieldDefinition? GetOrNullChild(string name)
    {
        return _children.GetValueOrDefault(name);
    }

    public FieldDefinition WithSelectList(FieldDefinitionValueSelectList selectList)
    {
        SelectList = selectList;

        return this;
    }

    public string GetPropertyPath()
    {
        var path = new StringBuilder(Name);
        var parent = Parent;

        while (parent != null)
        {
            path.Insert(0, parent.Name + '.');
            parent = parent.Parent;
        }

        return path.ToString();
    }

    public object? ValidateValue(object? value)
    {
        if (value == null) return null;

        var valueType = value.GetType();

        if (Type == FieldObjectType && valueType == FieldObjectType)
        {
            return value;
        }

        if (Type == FieldArrayType && valueType == FieldArrayType)
        {
            return value;
        }

        try
        {
            return FieldValueDictionary.ChangeType(value, Type);
        }
        catch
        {
            throw new ArgumentException($"Field \"{Name}\" has invalid value type {valueType.Name}, value type must be: {Type.Name}");
        }
    }

    public override string ToString()
    {
        return $"[{nameof(FieldDefinition)} {Name}]";
    }
}