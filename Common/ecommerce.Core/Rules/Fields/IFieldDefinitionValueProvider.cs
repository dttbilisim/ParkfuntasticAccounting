namespace ecommerce.Core.Rules.Fields;

public interface IFieldDefinitionValueProvider
{
    Task<object?> GetAsync(FieldDefinition fieldDefinition);
}