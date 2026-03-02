using System.Globalization;
using ecommerce.Core.Rules.Fields;

namespace ecommerce.Domain.Shared.Rules.OptionProviders;

public class WeekdayOptionProvider : FieldDefinitionValueOptionProvider
{
    public override Task<FieldDefinitionValueSelectPagedList> GetAsync(
        FieldDefinition fieldDefinition,
        int skip = 0,
        int take = 10,
        string? search = null,
        string[]? selected = null)
    {
        var dtif = CultureInfo.CurrentCulture.DateTimeFormat;

        var options = Enum.GetValues(typeof(DayOfWeek))
            .Cast<DayOfWeek>()
            .Select(x => new FieldDefinitionValueSelectListOption(dtif.GetDayName(x), ((int) x).ToString()))
            .ToArray();

        return Task.FromResult(
            new FieldDefinitionValueSelectPagedList
            {
                Data = options
            }
        );
    }
}