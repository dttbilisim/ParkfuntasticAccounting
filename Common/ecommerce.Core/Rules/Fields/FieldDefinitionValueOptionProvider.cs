namespace ecommerce.Core.Rules.Fields;

public abstract class FieldDefinitionValueOptionProvider : IFieldDefinitionValueOptionProvider
{
    public abstract Task<FieldDefinitionValueSelectPagedList> GetAsync(
        FieldDefinition fieldDefinition,
        int skip = 0,
        int take = 10,
        string? search = null,
        string[]? selected = null);
}