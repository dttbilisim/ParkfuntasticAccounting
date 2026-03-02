namespace ecommerce.Core.Rules.Fields;

public interface IFieldDefinitionContext
{
    IServiceProvider ServiceProvider { get; }

    FieldScopeDefinition GetScope(string name);

    FieldScopeDefinition? GetScopeOrNull(string name);

    FieldScopeDefinition AddScope(string name, string? displayName = null);

    void RemoveScope(string name);

    public FieldDefinition GetField(string name);

    FieldDefinition? GetFieldOrNull(string name);
}