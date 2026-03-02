using ecommerce.Odaksodt.Dtos.Invoice;
using FluentValidation;

namespace ecommerce.Odaksodt.Validators;

/// <summary>
/// Fatura oluşturma request validator
/// </summary>
public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequestDto>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.InvoiceType)
            .NotEmpty().WithMessage("Fatura tipi boş olamaz");

        RuleFor(x => x.InvoiceScenario)
            .NotEmpty().WithMessage("Fatura senaryosu boş olamaz");

        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage("Fatura numarası boş olamaz")
            .MaximumLength(50).WithMessage("Fatura numarası en fazla 50 karakter olabilir");

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("Fatura tarihi boş olamaz")
            .LessThanOrEqualTo(DateTime.Now.AddDays(1)).WithMessage("Fatura tarihi gelecek tarih olamaz");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Para birimi boş olamaz")
            .Length(3).WithMessage("Para birimi 3 karakter olmalıdır (TRY, USD, EUR)");

        RuleFor(x => x.Customer)
            .NotNull().WithMessage("Müşteri bilgileri boş olamaz")
            .SetValidator(new InvoiceCustomerValidator());

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Fatura kalemleri boş olamaz")
            .Must(lines => lines.Count > 0).WithMessage("En az bir fatura kalemi olmalıdır");

        RuleForEach(x => x.Lines)
            .SetValidator(new InvoiceLineValidator());
    }
}

/// <summary>
/// Müşteri bilgileri validator
/// </summary>
public class InvoiceCustomerValidator : AbstractValidator<InvoiceCustomerDto>
{
    public InvoiceCustomerValidator()
    {
        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi/TC kimlik numarası boş olamaz")
            .Must(BeValidTaxNumber).WithMessage("Geçersiz vergi/TC kimlik numarası");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Müşteri adı boş olamaz")
            .MaximumLength(200).WithMessage("Müşteri adı en fazla 200 karakter olabilir");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres boş olamaz")
            .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şehir boş olamaz")
            .MaximumLength(100).WithMessage("Şehir en fazla 100 karakter olabilir");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Geçersiz e-posta adresi");
    }

    private bool BeValidTaxNumber(string taxNumber)
    {
        // VKN 10 haneli, TC kimlik 11 haneli olmalı
        if (string.IsNullOrWhiteSpace(taxNumber))
            return false;

        var cleaned = taxNumber.Trim();
        return (cleaned.Length == 10 || cleaned.Length == 11) && cleaned.All(char.IsDigit);
    }
}

/// <summary>
/// Fatura kalemi validator
/// </summary>
public class InvoiceLineValidator : AbstractValidator<InvoiceLineDto>
{
    public InvoiceLineValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün/hizmet adı boş olamaz")
            .MaximumLength(300).WithMessage("Ürün/hizmet adı en fazla 300 karakter olabilir");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miktar 0'dan büyük olmalıdır");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Birim fiyat 0 veya daha büyük olmalıdır");

        RuleFor(x => x.VatRate)
            .GreaterThanOrEqualTo(0).WithMessage("KDV oranı 0 veya daha büyük olmalıdır")
            .LessThanOrEqualTo(100).WithMessage("KDV oranı 100'den küçük veya eşit olmalıdır");

        RuleFor(x => x.DiscountRate)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountRate.HasValue)
            .WithMessage("İskonto oranı 0 veya daha büyük olmalıdır")
            .LessThanOrEqualTo(100).When(x => x.DiscountRate.HasValue)
            .WithMessage("İskonto oranı 100'den küçük veya eşit olmalıdır");
    }
}
