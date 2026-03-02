using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ecommerce.Core.Helpers
{
    public static class EnumHelpers
    {
        public static string GetEnumDescription(this Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field != null && Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                return attribute.Description;
            }
            return enumValue.ToString();
        }

        public static string GetEnumDisplayName(this Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field != null)
            {
                if (Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) is DisplayAttribute displayAttribute)
                {
                    return displayAttribute.Name ?? enumValue.ToString();
                }
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute descriptionAttribute)
                {
                    return descriptionAttribute.Description;
                }
            }
            return enumValue.ToString();
        }
    }
}
