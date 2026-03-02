using ecommerce.Core.Entities.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace ecommerce.Admin.Domain.Dtos.Identity;

public class IdentityUserUpsertDtoValidator : AbstractValidator<IdentityUserUpsertDto>
{
    public IdentityUserUpsertDtoValidator()
    {
        RuleFor(t => t.UserName).NotEmpty().MaximumLength(256).WithName("Kullanıcı Adı");
        RuleFor(t => t.FirstName).NotNull().MaximumLength(64).Matches(@"^[\p{L}\p{N}\p{Zs}\.]+$").When(x => !string.IsNullOrEmpty(x.FirstName)).WithName("Ad");
        RuleFor(t => t.LastName).NotNull().MaximumLength(64).Matches(@"^[\p{L}\p{N}\p{Zs}\.]+$").When(x => !string.IsNullOrEmpty(x.LastName)).WithName("Soyad");
        RuleFor(t => t.Email).NotEmpty().MaximumLength(256).WithName("Email");
        RuleFor(t => t.PhoneNumber).Matches(@"^\d{7,12}$").When(d => !string.IsNullOrEmpty(d.PhoneNumber)).WithName("Telefon");
        RuleFor(t => t.Password).NotEmpty().MaximumLength(128).When(d => d.Id == null || !string.IsNullOrEmpty(d.Password)).WithName("Şifre");
        RuleFor(t => t.PasswordConfirm).Equal(t => t.Password).When(d => !string.IsNullOrEmpty(d.Password)).WithName("Şifre Tekrar");
        RuleFor(t => t.Roles).NotEmpty().WithName("Roller");
    }
}

public class CustomIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) => new IdentityError { Code = "PasswordTooShort", Description = $"Şifreler en az {length} karakter olmalıdır." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError { Code = "PasswordRequiresNonAlphanumeric", Description = $"Parolalarda en az bir alfasayısal olmayan karakter bulunmalıdır." };
    public override IdentityError PasswordRequiresLower() => new IdentityError { Code = "PasswordRequiresLower", Description = $"Şifrelerde en az bir küçük harf ('a'-'z') bulunmalıdır." };
    public override IdentityError PasswordRequiresUpper() => new IdentityError { Code = "PasswordRequiresUpper", Description = $"Şifrelerde en az bir büyük harf ('A'-'Z') bulunmalıdır." };
    public override IdentityError PasswordRequiresDigit() => new IdentityError { Code = "PasswordRequiresDigit", Description = $"Şifreler en az bir rakamdan oluşmalıdır ('0'-'9')" };

}