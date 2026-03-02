using System.Globalization;
using ecommerce.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ecommerce.Cargo.Mng;

public class MultiFormatDateConverter : IsoDateTimeConverter
{
    public List<string>? DateTimeFormats { get; set; }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var dateString = reader.Value.ToString();

        var isNullable = TypeHelper.IsNullable(objectType);

        if (existingValue == null)
        {
            return null;
        }

      
        if (dateString != null && DateTimeFormats?.Any() == true)
        {
            foreach (var format in DateTimeFormats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return date;
                }
            }
        }

        return base.ReadJson(reader, objectType, existingValue, serializer);
    }
}