using ecommerce.Core.Utils;
using FluentValidation;

namespace ecommerce.Admin.Domain.Dtos.PopupDto;

public class PopupValidator : AbstractValidator<PopupUpsertDto>
{
    public PopupValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithName("Popup Adı");
        RuleFor(x => x.Body).NotEmpty().WithName("İçerik");
        RuleFor(x => x.EndDate).Must((x, y) => x.StartDate == null || y == null || x.StartDate < y).WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.").WithName("Bitiş Tarihi");
        RuleFor(x => x.TriggerReference).NotEmpty().When(d => d.Trigger == PopupTrigger.Click).WithName("Tetikleme Referansı");
        RuleFor(x => x.Width).Matches(@"^\d+(px|%)$").WithMessage("Değer 100px veya 100% gibi olmalıdır.").When(d => !string.IsNullOrEmpty(d.Width)).WithName("Genişlik");
        RuleFor(x => x.Height).Matches(@"^\d+(px|%)$").WithMessage("Değer 100px veya 100% gibi olmalıdır.").When(d => !string.IsNullOrEmpty(d.Height)).WithName("Yükseklik");
    }
}