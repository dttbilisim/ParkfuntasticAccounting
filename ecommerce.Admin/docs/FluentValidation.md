# FluentValidation Kullanım Rehberi (ecommerce.Admin)

**Önemli:** Bu projede form/modal validasyonları için **her zaman FluentValidation** kullanılır. RadzenRequiredValidator veya manuel `if` kontrolleri yerine DTO için `AbstractValidator` yazılır ve formda FluentValidation entegre edilir.

---

## 1. Validator nereye yazılır?

- **Admin UI DTO'ları:** `ecommerce.Admin/ConfigureValidators/Validations.cs` içinde nested class olarak.
  - Örnek: `Validations.PriceListUpsertDtoValidator`, `Validations.CustomerUpsertDtoValidator`
- **Admin.Services / Domain DTO'ları:** İlgili Dto klasöründe `XxxValidator.cs` (örn. `ecommerce.Admin.Services/Dtos/PriceListDto/PriceListUpsertDtoValidator.cs`).
  - Not: `AddValidatorsFromAssemblies` şu an `Validations.Assembly` ve `ecommerce.Admin.Domain` yüklüyor; Admin.Services assembly’si ekliyse oradaki validator’lar da otomatik kayıt olur.

---

## 2. Validator kaydı

`ecommerce.Admin/AppStart/ConfigureServices.cs` içinde:

```csharp
builder.Services.AddValidatorsFromAssemblies(
    new[] { typeof(Validations).Assembly, Assembly.Load("ecommerce.Admin.Domain") },
    filter: result => result.ValidatorType.GetCustomAttribute<DisableFluentValidatorRegistrationAttribute>() == null);
```

Yeni bir assembly’den validator kullanacaksanız bu diziye ekleyin.

---

## 3. Validator yazma örneği

`ConfigureValidators/Validations.cs` içinde:

```csharp
public class PriceListUpsertDtoValidator : AbstractValidator<PriceListUpsertDto>
{
    public PriceListUpsertDtoValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Code).NotEmpty().WithMessage("Sirkü No zorunludur.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Sirkü Adı zorunludur.");
        RuleFor(x => x.StartDate).NotNull().WithMessage("Sirkü Tarihi zorunludur.");
        RuleFor(x => x.CorporationId).NotNull().GreaterThan(0).WithMessage("Şirket seçimi zorunludur.");
        RuleFor(x => x.BranchId).NotNull().GreaterThan(0).WithMessage("Şube seçimi zorunludur.");
        RuleFor(x => x.WarehouseId).NotNull().GreaterThan(0).WithMessage("Depo seçimi zorunludur.");
        RuleFor(x => x.CurrencyId).NotNull().GreaterThan(0).WithMessage("Döviz tipi zorunludur.");
    }
}
```

- `RuleFor` + `NotEmpty` / `NotNull` / `GreaterThan(0)` / `Must()` / `Custom()` kullanın.
- Mesajlar Türkçe ve alan adına uygun olsun.
- Karma kurallar için `RuleFor(x => x).Custom((dto, context) => { ... context.AddFailure("PropertyName", "Mesaj"); })` kullanılabilir.

---

## 4. Blazor’da kullanım

### Seçenek A: Blazored.FluentValidation (RadzenTemplateForm)

Form içinde validator’ı ekleyin, submit’te geçerlilik kontrolü yapın:

```razor
<RadzenTemplateForm TItem="PriceListUpsertDto" Data="@PriceList" Submit="@FormSubmit" OnInvalidSubmit="ShowErrors">
    <FluentValidationValidator @ref="_fluentValidationValidator"/>
    <!-- alanlar -->
</RadzenTemplateForm>
```

Code-behind:

```csharp
using Blazored.FluentValidation;

private FluentValidationValidator? _fluentValidationValidator;

private async Task FormSubmit(PriceListUpsertDto args)
{
    if (_fluentValidationValidator?.Validate() == false)
        return;
    // kaydet
}

private void ShowErrors() => NotificationService.Notify(NotificationSeverity.Warning, "Lütfen zorunlu alanları doldurun.");
```

### Seçenek B: RadzenFluentValidator (EditContext ile)

`RadzenTemplateForm` içinde tek bir `RadzenFluentValidator<TItem>` kullanılabilir; validator DI’dan veya `Validator` parametresiyle gelir. Örnek:

```razor
<RadzenFluentValidator TItem="PriceListUpsertDto" Name="Code" />
```

Validator, `IValidator<PriceListUpsertDto>` olarak DI’da kayıtlı olmalı (AddValidatorsFromAssemblies ile kayıt yeterli).

---

## 5. Hatırlanacaklar

- Yeni bir upsert/modal formu eklendiğinde: Önce DTO için FluentValidation validator yazın (Validations.cs veya Dto klasöründe), sonra formda FluentValidation kullanın.
- RadzenRequiredValidator ile FluentValidation birlikte kullanılabilir; ancak tek kaynak FluentValidation tercih edilir ki kurallar tek yerde kalsın.
- Tenant/şube alanları (CorporationId, BranchId, WarehouseId) zorunluysa validator’da mutlaka tanımlayın. Proje tenant yapısı için: `docs/TenantStructure.md`.
- Mesaj metinleri Türkçe ve kullanıcıya net olsun.

---

## 6. Referans sayfalar

- `UpsertCompany.razor` + `Validations.CompanyUpsertDtoDtoValidator`
- `UpsertCustomer.razor` + `Validations.CustomerUpsertDtoValidator`
- `UpsertProduct.razor` + Blazored.FluentValidation
- `ecommerce.Admin/Components/Layout/RadzenFluentValidator.razor` (EditContext entegrasyonu)
