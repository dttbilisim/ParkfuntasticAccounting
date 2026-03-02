using ecommerce.Core.Entities;
using ecommerce.Domain.Shared.Dtos.SupportLine;
using FluentValidation;

namespace ecommerce.Web.Validators;

public class SupportLineValidator : AbstractValidator<SupportLine>
{
    public SupportLineValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası gereklidir.")
            .Matches(@"^\d{10,15}$").WithMessage("Geçerli bir telefon numarası giriniz.");

        RuleFor(x => x.FrequentlyAskedQuestionsId)
            .GreaterThan(0).WithMessage("Sıkça Sorulan Sorular alanı zorunludur.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Açıklama alanı boş bırakılamaz.");

        RuleFor(x => x.SupportLineReturnType)
            .NotNull().WithMessage("Dönüş tipi seçilmelidir.");

        RuleFor(x => x.SupportLineType)
            .NotNull().WithMessage("Destek hattı tipi seçilmelidir.");
    }
}