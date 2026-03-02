using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils;
using FluentValidation;

namespace ecommerce.Web.Validators;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.PasswordHash)
            .NotEmpty().WithMessage("Şifre gereklidir.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

        RuleFor(x => x.BirthDate)
            .NotNull().WithMessage("Doğum tarihi gereklidir.")
            .LessThan(DateTime.Today).WithMessage("Doğum tarihi bugünden büyük olamaz.")
            .Must(date => date <= DateTime.Today.AddYears(-18)).WithMessage("Kullanıcı en az 18 yaşında olmalıdır.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası gereklidir.")
            .Matches(@"^\d{10,15}$").WithMessage("Telefon numarası geçerli değil.");

        When(x => x.WebUserType == WebUserType.B2B, () =>
        {
            RuleFor(x => x.CompanyName)
                .NotEmpty().WithMessage("Firma adı tedarikçiler  için zorunludur.");

            RuleFor(x => x.VatNumber)
                .NotEmpty().WithMessage("Vergi dairesi tedarikçiler için zorunludur.")
                .Length(10).WithMessage("Vergi numarası 10 haneli olmak zorundadır.");

            RuleFor(x => x.VatOffice)
                .NotEmpty().WithMessage("Vergi numarası tedarikçiler  için zorunludur.");
        });
    }
}