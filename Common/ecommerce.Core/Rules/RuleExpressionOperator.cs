using System.ComponentModel.DataAnnotations;

namespace ecommerce.Core.Rules;

public enum RuleExpressionOperator
{
    [Display(Description = "Eşittir")]
    Equal,

    [Display(Description = "Eşit Değildir")]
    NotEqual,

    [Display(Description = "Büyüktür")]
    GreaterThan,

    [Display(Description = "Büyük veya Eşittir")]
    GreaterThanOrEqual,

    [Display(Description = "Küçüktür")]
    LessThan,

    [Display(Description = "Küçük veya Eşittir")]
    LessThanOrEqual,

    [Display(Description = "İle Başlar")]
    StartsWith,

    [Display(Description = "İle Biter")]
    EndsWith,

    [Display(Description = "İçerir")]
    Contains,

    [Display(Description = "İçermez")]
    NotContains,

    [Display(Description = "İçinde")]
    In,

    [Display(Description = "İçinde Değil")]
    NotIn,

    [Display(Description = "Boş Değil")]
    IsNotNull,

    [Display(Description = "Boş")]
    IsNull,
}