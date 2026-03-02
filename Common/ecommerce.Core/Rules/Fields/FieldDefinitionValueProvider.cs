namespace ecommerce.Core.Rules.Fields;

public abstract class FieldDefinitionValueProvider : IFieldDefinitionValueProvider
{
    public abstract Task<object?> GetAsync(FieldDefinition fieldDefinition);
}