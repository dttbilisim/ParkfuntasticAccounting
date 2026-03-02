using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ecommerce.Core.Rules;

public enum RuleDateTimeExpressionUnit
{
    [EnumMember(Value = "s")]
    [Display(Description = "Saniye")]
    Second,

    [EnumMember(Value = "m")]
    [Display(Description = "Dakika")]
    Minute,

    [EnumMember(Value = "h")]
    [Display(Description = "Saat")]
    Hour,

    [EnumMember(Value = "d")]
    [Display(Description = "Gün")]
    Day,

    [EnumMember(Value = "w")]
    [Display(Description = "Hafta")]
    Week,

    [EnumMember(Value = "M")]
    [Display(Description = "Ay")]
    Month,

    [EnumMember(Value = "y")]
    [Display(Description = "Yıl")]
    Year,
}