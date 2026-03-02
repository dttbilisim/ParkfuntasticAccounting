using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Rules.Providers;
using FluentValidation;

namespace ecommerce.Admin.Domain.Dtos.DiscountDto;

public class DiscountValidator : AbstractValidator<DiscountUpsertDto>
{
    public DiscountValidator(IFieldDefinitionManager fieldDefinitionManager)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name).NotEmpty().WithName("İndirim Adı");
        RuleFor(x => x.DiscountType).IsInEnum().WithName("İndirim Tipi");
        RuleFor(x => x.DiscountPercentage).InclusiveBetween(0, 100).NotNull().When(x => x.UsePercentage).WithMessage("Lütfen indirim oranı giriniz!").WithName("İndirim Oranı");
        RuleFor(x => x.DiscountAmount).NotNull().When(x => !x.UsePercentage).WithMessage("Lütfen indirim tutarı giriniz!").WithName("İndirim Tutarı");
        RuleFor(x => x.StartDate).NotNull().WithName("Başlangıç Tarihi");
        RuleFor(x => x.EndDate).NotNull().Must((x, y) => x.StartDate == null || y == null || x.StartDate < y).WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz!").WithName("Bitiş Tarihi");
        RuleFor(x => x.DiscountLimitation).IsInEnum().WithName("İndirim Limiti");
        RuleFor(x => x.LimitationTimes).GreaterThan(0).When(x => x.DiscountLimitation != DiscountLimitationType.Unlimited).WithName("Sınırlama Sayısı");
        RuleFor(x => x.GiftProductIds).NotEmpty().When(x => x.HasGiftProducts).WithName("Hediye Ürünler");
        RuleFor(x => x.RequiresCouponCode).Must((x, y) => !y || !string.IsNullOrWhiteSpace(x.CouponCode) || x.CompanyCoupons.Any()).WithMessage("Kupon gerekliyken, genel kupon kodu veya şirket kuponu tanımlanmalıdır!").WithName("Kupon Kodu");

        var ruleScope = fieldDefinitionManager.GetScope(DiscountFieldDefinitions.Scope);
        RuleFor(x => x.Rule).SetValidator(new RuleValidator(ruleScope)!).When(x => x.Rule?.Field != null || x.Rule?.Children.Any() == true).WithName("Kural");

        RuleFor(x => x.CompanyCoupons)
            .Must(x => x.Select(c => c.CompanyId).Distinct().Count() == x.Count)
            .WithMessage("Aynı şirkete ait birden fazla kupon eklenemez!")
            .WithName("Şirket Kuponları");

        RuleForEach(x => x.CompanyCoupons).SetValidator(new DiscountCompanyCouponValidator());
    }
}