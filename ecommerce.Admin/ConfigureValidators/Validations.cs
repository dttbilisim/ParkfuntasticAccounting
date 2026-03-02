using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Admin.Domain.Dtos.CompanyCargoDto;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.FrequentlyAskedQuestionsDto;
using ecommerce.Admin.Domain.Dtos.ProductActiveArcticleDto;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Utils;
using FluentValidation;
using ecommerce.Admin.Domain.Dtos.EditorialContentDto;
using ecommerce.Admin.Domain.Dtos.SurveyDto;
using ecommerce.Admin.Domain.Dtos.ProductUnitDto;

namespace ecommerce.Admin.ConfigureValidators
{
    public class Validations
    {

        public class CompanyCargoUpsertDtoDtoValidator : AbstractValidator<CompanyCargoUpsertDto>
        {
            public CompanyCargoUpsertDtoDtoValidator()
            {
                RuleFor(x => x.CargoId).NotNull().WithMessage("Kargo-Boş Geçilemez.");
                RuleFor(x => x.MinBasketAmount).NotNull().WithMessage("Min. Sepet Tutarı-Boş Geçilemez.");
            }
        }

        public class ProductUpsertDtoDtoValidator : AbstractValidator<ProductUpsertDto>
        {
            public ProductUpsertDtoDtoValidator()
            {
                RuleFor(x => x.Name).NotNull().WithMessage("Ürün Adı-Boş Geçilemez.");

                RuleFor(x => x.CartMinValue).NotNull().WithMessage("Kart Min. Değer-Boş Geçilemez.");
                RuleFor(x => x.Weight).NotNull().WithMessage("Ağırlık-Boş Geçilemez.");
                RuleFor(x => x.Width).NotNull().WithMessage("Genişlik-Boş Geçilemez.");
                RuleFor(x => x.Length).NotNull().WithMessage("Uzunluk-Boş Geçilemez.");
                RuleFor(x => x.Height).NotNull().WithMessage("Yükseklik-Boş Geçilemez.");
                RuleFor(x => x.BrandId).NotNull().WithMessage("Marka-Boş Geçilemez.");
                RuleFor(x => x.TaxId).NotNull().WithMessage("Vergi-Boş Geçilemez.");

            }
        }

        public class CompanyUpsertDtoDtoValidator : AbstractValidator<CompanyUpsertDto>
        {
            public CompanyUpsertDtoDtoValidator()
            {
                RuleFor(x => x).Custom((val, context) =>
                {
                    if (val.CompanyWorkingType == 0)
                        context.AddFailure("Çalışma Tipi-Boş Geçilemez.");

                    if (val.UserType == 0)
                        context.AddFailure("Kullanıcı Tipi-Boş Geçilemez.");

                    if (val.CityId == 0)
                        context.AddFailure("Şehir-Boş Geçilemez.");

                    if (val.TownId == 0)
                        context.AddFailure("İlçe-Boş Geçilemez.");


                    if (string.IsNullOrEmpty(val.EmailAddress))
                        context.AddFailure("EMail-Boş Geçilemez.");

                    if (string.IsNullOrEmpty(val.PhoneNumber))
                        context.AddFailure("Telefon Numarası-Boş Geçilemez.");


                    if (string.IsNullOrEmpty(val.FirstName) && string.IsNullOrEmpty(val.LastName))
                        context.AddFailure("Ad ve Soyad-Boş Geçilemez.");


                    if (string.IsNullOrEmpty(val.Iban) && val.CompanyWorkingType!=CompanyWorkingType.Buyer)
                        context.AddFailure("Iban-Boş Geçilemez.");

                    if (string.IsNullOrEmpty(val.TaxNumber) && val.CompanyWorkingType!=CompanyWorkingType.Buyer)
                        context.AddFailure("Vergi No-Boş Geçilemez.");

                    if (string.IsNullOrEmpty(val.TaxName) && val.CompanyWorkingType!=CompanyWorkingType.Buyer)
                        context.AddFailure("Vergi Dairesi-Boş Geçilemez.");

                 
                    
                    switch (val.UserType)
                    {
                        case UserType.Custormer:
                            {
                                if (string.IsNullOrEmpty(val.InvoiceAddress))
                                    context.AddFailure("Fatura Adres-Boş Geçilemez.");

                                if (string.IsNullOrEmpty(val.GlnNumber))
                                    context.AddFailure("Gln Numarası-Boş Geçilemez.");
                            }
                            break;
                        case UserType.BusinessPartner or UserType.BusinessPartner:
                            {
                                if (string.IsNullOrEmpty(val.AccountEmailAddress))
                                    context.AddFailure("Fatura Email Adresi-Boş Geçilemez.");
                            }
                            break;
                    }
                });
                RuleFor(x => x.GlnNumber).Matches(@"^\d{13}$").When(x => !string.IsNullOrEmpty(x.GlnNumber) && x.UserType == UserType.Custormer).WithMessage("Gln Numarası-Gln Numarası 13 haneli olmalıdır.");
            }
        }

        public class ProductActiveArticleUpsertValidator : AbstractValidator<ProductActiveArticleUpsertDto>
        {
            public ProductActiveArticleUpsertValidator()
            {
                RuleFor(x => x.ProductId).NotNull().WithMessage("Ürün Boş Geçilemez.");
                RuleFor(x => x.ActiveArticleId).NotNull().WithMessage("Etken Madde-Boş Geçilemez.");
                RuleFor(x => x.ScaleUnitId).NotNull().WithMessage("Ölçüm Birimi-Boş Geçilemez.");
                RuleFor(x => x.Amount).NotNull().WithMessage("Miktar-Boş Geçilemez.");
                RuleFor(x => x.ScaleCount).NotNull().WithMessage("Ölçek Adeti-Boş Geçilemez.");
                RuleFor(x => x.ScaleType).NotNull().WithMessage("Ölçüm Cinsi-Boş Geçilemez.");
            }
        }

        public class CargoUpsertValidator : AbstractValidator<CargoUpsertDto>
        {
            public CargoUpsertValidator()
            {
                RuleFor(x => x.Name).NotNull().WithMessage("Ad-Boş Geçilemez.");
            }
        }

        public class FAQBlogUpsertValidator : AbstractValidator<FrequentlyAskedQuestionUpsertDto>
        {
            public FAQBlogUpsertValidator()
            {
                RuleFor(x => x.Name).NotNull().WithMessage("Ad-Boş Geçilemez.");
                RuleFor(x => x.Group).NotNull().WithMessage("Grup-Boş Geçilemez.");
                RuleFor(x => x.Description).NotNull().WithMessage("İçerik-Boş Geçilemez.");
            }
        }

        public class SurveyValidator : AbstractValidator<SurveyUpsertDto>
        {
            public SurveyValidator()
            {
                RuleFor(x => x.Title).NotEmpty().WithName("Anket Başlığı");

                RuleForEach(x => x.SurveyOptions).SetValidator(new SurveyOptionValidator());
            }
        }

        public class SurveyOptionValidator : AbstractValidator<SurveyOptionUpsertDto>
        {
            public SurveyOptionValidator()
            {
                RuleFor(x => x.Title).NotEmpty().WithName("Seçenek");
            }
        }

        public class EditorialContentValidator : AbstractValidator<EditorialContentUpsertDto>
        {
            public EditorialContentValidator()
            {
                RuleLevelCascadeMode = CascadeMode.Stop;

                RuleFor(x => x.Type).NotNull().IsInEnum().WithName("İçerik Tipi");
                RuleFor(x => x.Title).NotEmpty().WithName("Başlık");
                RuleFor(x => x.Thumbnail).NotEmpty().WithName("Resim");
                RuleFor(x => x.PublishDate).NotNull().WithName("Yayın Tarihi");
                RuleFor(x => x.EndDate).NotNull().WithName("Bitiş Tarihi");
                RuleFor(x => x.Category).NotEmpty().MaximumLength(100).When(x => x.Type == EditorialContentType.Article).WithName("Banner Item");
                RuleFor(x => x.Video).NotEmpty().Must(s => Uri.TryCreate(s, UriKind.Absolute, out _)).When(x => x.Type == EditorialContentType.Video).WithName("Video Url");
            }
        }
        public class ProductUnitUpsertValidator : AbstractValidator<ProductUnitUpsertDto>
        {
            public ProductUnitUpsertValidator()
            {
                RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Ürün-seçimi zorunludur.");
                RuleFor(x => x.UnitId).GreaterThan(0).WithMessage("Birim-seçimi zorunludur.");
                RuleFor(x => x.UnitValue).NotNull().GreaterThan(0).WithMessage("Birim Değeri-0'dan büyük olmalıdır.");
                RuleFor(x => x.Barcode).MaximumLength(100).WithMessage("Barkod-100 karakterden uzun olamaz.");
            }
        }

        public class SellerItemUpsertDtoValidator : AbstractValidator<ecommerce.Admin.Domain.Dtos.SellerItemDto.SellerItemUpsertDto>
        {
            public SellerItemUpsertDtoValidator()
            {
                RuleFor(x => x.SellerId).GreaterThan(0).WithMessage("Satıcı-seçimi zorunludur.");
                RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Ürün-seçimi zorunludur.");
                RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("Stok-0'dan küçük olamaz.");
                RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).WithMessage("Satış Fiyatı-0'dan küçük olamaz.");
                RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).WithMessage("Maliyet Fiyatı-0'dan küçük olamaz.");
                RuleFor(x => x.Commision).InclusiveBetween(0, 100).WithMessage("Komisyon-0 ile 100 arasında olmalıdır.");
                RuleFor(x => x.Currency).NotEmpty().WithMessage("Para Birimi-boş geçilemez.");
                RuleFor(x => x.Unit).NotEmpty().WithMessage("Birim-boş geçilemez.");
            }
        }

        public class CustomerUpsertDtoValidator : AbstractValidator<CustomerUpsertDto>
        {
            public CustomerUpsertDtoValidator()
            {
                RuleLevelCascadeMode = CascadeMode.Stop;

                // Genel bilgiler
                RuleFor(x => x.Code).NotEmpty().WithMessage("Cari kodu zorunludur.");
                RuleFor(x => x.Name).NotEmpty().WithMessage("Cari adı zorunludur.");
                RuleFor(x => x.Type).IsInEnum().WithMessage("Cari tipi seçiniz.");
                RuleFor(x => x.RegionId).NotNull().GreaterThan(0).WithMessage("Bölge seçiniz.");
                RuleFor(x => x.TaxOffice).NotEmpty().WithMessage("Vergi dairesi zorunludur.");
                RuleFor(x => x.TaxNumber).NotEmpty().WithMessage("Vergi no / TC zorunludur.");
                RuleFor(x => x.TaxNumber)
                    .Must(t => string.IsNullOrWhiteSpace(t) || new string(t!.Where(char.IsDigit).ToArray()).Length is 10 or 11)
                    .WithMessage("Vergi numarası 10, TC kimlik numarası 11 haneli olmalıdır.")
                    .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));

                // İletişim adres
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("E-posta zorunludur.")
                    .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
                RuleFor(x => x).Custom((dto, context) =>
                {
                    if (string.IsNullOrWhiteSpace(dto.Mobile) && string.IsNullOrWhiteSpace(dto.Phone))
                        context.AddFailure("Mobile", "Cep telefonu veya sabit telefon alanlarından en az biri zorunludur.");
                });
                RuleFor(x => x.CityId).NotNull().GreaterThan(0).WithMessage("İl seçiniz.");
                RuleFor(x => x.TownId).NotNull().GreaterThan(0).WithMessage("İlçe seçiniz.");
                RuleFor(x => x.Address).NotEmpty().WithMessage("Adres zorunludur.");

                // Kurumsal cari: en az bir şube yetkisi
                RuleFor(x => x).Custom((dto, context) =>
                {
                    var isCorporate = dto.Type == CustomerType.Buyer || dto.Type == CustomerType.BuyerSeller;
                    if (isCorporate && (dto.Branches == null || !dto.Branches.Any()))
                        context.AddFailure("Branches", "Kurumsal cari için en az bir şube yetkisi seçiniz. Şube Yönetimi sekmesinden şube ekleyin.");
                });
            }
        }
    }
}
