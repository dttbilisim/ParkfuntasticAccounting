namespace ecommerce.Core.Rules.Fields;

public interface IFieldDefinitionValueOptionProvider
{
    Task<FieldDefinitionValueSelectPagedList> GetAsync(
        FieldDefinition fieldDefinition,
        int skip = 0,
        int take = 10,
        string? search = null,
        string[]? selected = null);
}