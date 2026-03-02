using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;

namespace ecommerce.Core.Extensions
{
    public static class EnumExtensions
    {
        public static string GetStringValue(this Enum enumVal)
        {
            var field = enumVal.GetType().GetField(enumVal.ToString());

            var enumMember = field?.GetCustomAttribute<EnumMemberAttribute>(false);

            return enumMember?.Value ?? field?.Name ?? enumVal.ToString();
        }

        public static string GetDisplayName(this Enum enumVal, bool preferEnumMember = false)
        {
            var field = enumVal.GetType().GetField(enumVal.ToString());

            if (preferEnumMember)
            {
                var enumMember = field?.GetCustomAttribute<EnumMemberAttribute>(false);

                if (enumMember?.Value != null)
                {
                    return enumMember.Value;
                }
            }

            var display = field?.GetCustomAttribute<DisplayAttribute>(false);

            return display?.GetDescription() ?? field?.Name ?? enumVal.ToString();
        }
    }
}