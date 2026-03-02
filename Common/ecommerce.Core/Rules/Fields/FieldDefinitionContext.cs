namespace ecommerce.Core.Rules.Fields;

public class FieldDefinitionContext : IFieldDefinitionContext
{
    public IServiceProvider ServiceProvider { get; }

    public Dictionary<string, FieldScopeDefinition> Scopes { get; }

    public FieldDefinitionContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Scopes = new Dictionary<string, FieldScopeDefinition>();
    }

    public FieldScopeDefinition AddScope(
        string name,
        string? displayName = null)
    {
        if (Scopes.ContainsKey(name))
        {
            throw new ArgumentException($"There is already an existing field scope with name: {name}");
        }

        return Scopes[name] = new FieldScopeDefinition(name, displayName);
    }

    public FieldScopeDefinition GetScope(string name)
    {
        var scope = GetScopeOrNull(name);

        if (scope == null)
        {
            throw new ArgumentException($"Could not find a field definition scope with the given name: {name}");
        }

        return scope;
    }

    public FieldScopeDefinition? GetScopeOrNull(string name)
    {
        if (!Scopes.ContainsKey(name))
        {
            return null;
        }

        return Scopes[name];
    }

    public void RemoveScope(string name)
    {
        if (!Scopes.ContainsKey(name))
        {
            throw new ArgumentException($"Not found field scope with name: {name}");
        }

        Scopes.Remove(name);
    }

    public FieldDefinition GetField(string name)
    {
        var field = GetFieldOrNull(name);

        if (field == null)
        {
            throw new ArgumentException("Undefined field: " + name);
        }

        return field;
    }

    public FieldDefinition? GetFieldOrNull(string name)
    {
        var paths = name.Split('.');
        FieldDefinition? field = null;

        foreach (var path in paths)
        {
            if (field == null)
            {
                foreach (var scopeDefinition in Scopes.Values)
                {
                    field = scopeDefinition.GetFieldOrNull(path);

                    if (field != null) break;
                }
            }
            else
            {
                field = field.GetOrNullChild(path);
            }

            if (field == null) break;
        }

        return field;
    }
}