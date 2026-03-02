using ecommerce.Core.Entities.Authentication;
using FluentValidation;

namespace ecommerce.Web.Validators;

public class UserAddressValidator : AbstractValidator<UserAddress>
{
    public UserAddressValidator()
    {
        RuleFor(x => x.AddressName)
            .NotEmpty().WithMessage("Adres ismi boş olamaz");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad Soyad boş olamaz");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası boş olamaz");

        RuleFor(x => x.CityId)
            .NotNull().WithMessage("İl seçilmelidir");

        RuleFor(x => x.TownId)
            .NotNull().WithMessage("İlçe seçilmelidir");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres detayı boş olamaz");

        // Invoice Address Validation - Only when IsSameAsDeliveryAddress is FALSE
        When(x => !x.IsSameAsDeliveryAddress, () =>
        {
            RuleFor(x => x.InvoiceCityId)
                .NotNull().WithMessage("Fatura ili seçilmelidir!");

            RuleFor(x => x.InvoiceTownId)
                .NotNull().WithMessage("Fatura ilçesi seçilmelidir!");

            RuleFor(x => x.InvoiceAddress)
                .NotEmpty().WithMessage("Fatura adres detayı girilmelidir!");
        });
    }
}
