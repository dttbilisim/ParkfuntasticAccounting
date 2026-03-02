using ecommerce.Core.Rules.Fields;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class TodayValueProvider : FieldDefinitionValueProvider
{
    public override Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        return Task.FromResult<object?>(DateOnly.FromDateTime(DateTime.Now));
    }
}