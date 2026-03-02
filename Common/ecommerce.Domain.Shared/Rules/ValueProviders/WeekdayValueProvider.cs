using ecommerce.Core.Rules.Fields;

namespace ecommerce.Domain.Shared.Rules.ValueProviders;

public class WeekdayValueProvider : FieldDefinitionValueProvider
{
    public override Task<object?> GetAsync(FieldDefinition fieldDefinition)
    {
        return Task.FromResult<object?>((int) DateTime.Now.DayOfWeek);
    }
}