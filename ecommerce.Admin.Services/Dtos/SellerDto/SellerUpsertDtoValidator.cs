using FluentValidation;

namespace ecommerce.Admin.Domain.Dtos.SellerDto;

public class SellerUpsertDtoValidator : AbstractValidator<SellerUpsertDto>
{
    public SellerUpsertDtoValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Satıcı adı boş bırakılamaz.")
            .MaximumLength(200).WithMessage("Satıcı adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş bırakılamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası boş bırakılamaz.")
            .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter olabilir.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres boş bırakılamaz.");

        RuleFor(x => x.Commission)
            .InclusiveBetween(0, 100).WithMessage("Komisyon oranı 0 ile 100 arasında olmalıdır.");

        RuleFor(x => x.CityId)
            .NotEmpty().WithMessage("Lütfen bir şehir seçiniz.");

        RuleFor(x => x.TownId)
            .NotEmpty().WithMessage("Lütfen bir ilçe seçiniz.");

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi numarası boş bırakılamaz.")
            .MaximumLength(20).WithMessage("Vergi numarası en fazla 20 karakter olabilir.");

        RuleFor(x => x.TaxOffice)
            .NotEmpty().WithMessage("Vergi dairesi boş bırakılamaz.")
            .MaximumLength(100).WithMessage("Vergi dairesi en fazla 100 karakter olabilir.");
    }
}
