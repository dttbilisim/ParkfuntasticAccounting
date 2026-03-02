using System.Collections.Immutable;

namespace ecommerce.Core.Rules.Fields;

public class FieldScopeDefinition
{
    public string Name { get; }

    public string DisplayName { get; }

    public IReadOnlyList<FieldDefinition> Fields => _fields.ToImmutableList();

    private readonly List<FieldDefinition> _fields;

    public FieldScopeDefinition(string name, string? displayName = null)
    {
        Name = name;
        DisplayName = displayName ?? name;

        _fields = new List<FieldDefinition>();
    }

    public FieldDefinition AddField(
        string name,
        Type type,
        Type valueProviderType,
        string? displayName = null,
        bool isEnabled = true)
    {
        var field = new FieldDefinition(
            name,
            type,
            valueProviderType,
            displayName,
            isEnabled
        );

        _fields.Add(field);

        return field;
    }

    public FieldDefinition GetField(string name)
    {
        var field = GetFieldOrNull(name);

        if (field == null)
        {
            throw new Exception("Undefined field: " + name);
        }

        return field;
    }

    public FieldDefinition? GetFieldOrNull(string name)
    {
        return _fields.FirstOrDefault(f => f.Name == name);
    }

    public override string ToString()
    {
        return $"[{nameof(FieldScopeDefinition)} {Name}]";
    }
}