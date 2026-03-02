using System.Globalization;
using Newtonsoft.Json.Linq;

namespace ecommerce.Core.Extensions
{
    public static class QueryStringExtensions
    {
        public static string AddQueryParam(this string original, string name, object value)
        {
            original += original.Contains('?') ? "&" : "?";

            var path = string.Join(
                "",
                name.Split('.').Select((s, i) => i == 0 ? s : $"[{s}]")
            );

            original += $"{path}={value}";

            return original;
        }

        public static IDictionary<string, string?> ToKeyValueString(this object? metaToken)
        {
            var result = new Dictionary<string, string?>();

            if (metaToken == null)
            {
                return result;
            }

            if (metaToken is not JToken token)
            {
                return ToKeyValueString(JObject.FromObject(metaToken));
            }

            if (token.HasValues)
            {
                var contentData = new Dictionary<string, string?>();
                foreach (var child in token.Children().ToList())
                {
                    var childContent = child.ToKeyValueString();
                    contentData = contentData.Concat(childContent)
                        .ToDictionary(k => k.Key, v => v.Value);
                }

                return contentData;
            }

            var jValue = token as JValue;
            if (jValue?.Value == null)
            {
                return result;
            }

            var value = jValue.Type == JTokenType.Date
                ? jValue.ToString("o", CultureInfo.InvariantCulture)
                : jValue.ToString(CultureInfo.InvariantCulture);

            var path = string.Join(
                "",
                token.Path.Split('.').Select((s, i) => i == 0 ? s : $"[{s}]")
            );

            return new Dictionary<string, string?> { { path, value } };
        }
    }
}