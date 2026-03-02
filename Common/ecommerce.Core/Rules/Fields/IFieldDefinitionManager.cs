namespace ecommerce.Core.Rules.Fields;

public interface IFieldDefinitionManager
{
    FieldDefinition GetField(string scope, string name);

    FieldDefinition? GetFieldOrNull(string scope, string name);

    FieldScopeDefinition GetScope(string name);

    FieldScopeDefinition? GetScopeOrNull(string name);

    IReadOnlyList<FieldDefinition> GetFields(string scope);

    IReadOnlyList<FieldScopeDefinition> GetScopes();
}