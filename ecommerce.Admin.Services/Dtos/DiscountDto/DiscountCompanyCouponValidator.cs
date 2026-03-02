using FluentValidation;

namespace ecommerce.Admin.Domain.Dtos.DiscountDto;

public class DiscountCompanyCouponValidator : AbstractValidator<DiscountCompanyCouponUpsertDto>
{
    public DiscountCompanyCouponValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithName("Şirket");
        RuleFor(x => x.CouponCode).NotEmpty().WithName("Kupon Kodu");
    }
}