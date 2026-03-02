# Odaksoft E-Fatura Entegrasyonu

Bu proje, Odaksoft E-Dönüşüm platformu ile entegrasyon için geliştirilmiş temiz mimari yapısına sahip bir .NET 9 kütüphanesidir.

## 📋 Özellikler

- ✅ Temiz mimari (Abstract/Concrete pattern)
- ✅ FluentValidation ile input validasyonu
- ✅ Polly ile retry ve circuit breaker politikaları
- ✅ Token yönetimi (otomatik yenileme)
- ✅ Comprehensive logging
- ✅ Dependency Injection desteği
- ✅ AppSettings ile konfigürasyon

## 🏗️ Proje Yapısı

```
ecommerce.Odaksodt/
├── Abstract/                    # Interface tanımları
│   ├── IOdaksoftAuthService.cs
│   ├── IOdaksoftHttpClient.cs
│   └── IOdaksoftInvoiceService.cs
├── Concreate/                   # Implementasyonlar
│   ├── OdaksoftAuthService.cs
│   ├── OdaksoftHttpClient.cs
│   └── OdaksoftInvoiceService.cs
├── Dtos/                        # Data Transfer Objects
│   ├── Auth/
│   ├── Invoice/
│   └── Common/
├── Options/                     # Konfigürasyon sınıfları
│   └── OdaksoftOptions.cs
├── Validators/                  # FluentValidation validatorları
│   └── CreateInvoiceRequestValidator.cs
├── Extensions/                  # Extension metodları
│   └── ServiceCollectionExtensions.cs
└── swagger/                     # API dokümantasyonu
    └── swagger.json
```

## 🚀 Kurulum

### 1. NuGet Paketleri

Proje otomatik olarak gerekli paketleri içerir:
- Microsoft.Extensions.Http
- Microsoft.Extensions.Http.Polly
- FluentValidation
- Polly

### 2. AppSettings Konfigürasyonu

`appsettings.json` dosyanıza aşağıdaki konfigürasyonu ekleyin:

```json
{
  "Odaksoft": {
    "BaseUrl": "https://integration-test.odaksoft.com.tr/api/",
    "Username": "KullaniciAdiniz",
    "Password": "Sifreniz",
    "TokenExpirationMinutes": 60,
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "IsTestEnvironment": true
  }
}
```

**Test Ortamı:**
- BaseUrl: `https://integration-test.odaksoft.com.tr/api/`
- Portal: `https://portal-test.odaksoft.com.tr`
- Test Kullanıcı: `OdaksoftWS` / `OdaksoftWS`

**Canlı Ortam:**
- BaseUrl: `https://integration.odaksoft.com.tr/api/`
- Portal: `https://portal.odaksoft.com.tr`

### 3. Dependency Injection

`Program.cs` veya `Startup.cs` dosyanızda servisleri kaydedin:

```csharp
using ecommerce.Odaksodt.Extensions;

// Servisleri ekle
builder.Services.AddOdaksoftServices(builder.Configuration);
```

## 💻 Kullanım Örnekleri

### Fatura Oluşturma

```csharp
public class OrderService
{
    private readonly IOdaksoftInvoiceService _invoiceService;

    public OrderService(IOdaksoftInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public async Task<string> CreateInvoiceAsync(Order order)
    {
        var request = new CreateInvoiceRequestDto
        {
            InvoiceType = "SATIS",
            InvoiceScenario = "TEMELFATURA",
            InvoiceNumber = order.OrderNumber,
            InvoiceDate = DateTime.Now,
            Currency = "TRY",
            ReferenceNumber = order.Id.ToString(),
            Customer = new InvoiceCustomerDto
            {
                TaxNumber = order.Customer.TaxNumber,
                Name = order.Customer.CompanyName,
                Address = order.Customer.Address,
                City = order.Customer.City,
                Email = order.Customer.Email
            },
            Lines = order.Items.Select(item => new InvoiceLineDto
            {
                Name = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                VatRate = item.VatRate,
                Unit = "Adet"
            }).ToList()
        };

        var response = await _invoiceService.CreateInvoiceAsync(request);
        
        if (response.Success)
        {
            return response.Ettn; // ETTN'i kaydedin
        }
        
        throw new Exception(response.ErrorMessage);
    }
}
```

### Fatura Durumu Sorgulama

```csharp
public async Task<InvoiceStatusResponseDto> CheckInvoiceStatusAsync(string ettn)
{
    var request = new InvoiceStatusRequestDto
    {
        EttnList = new List<string> { ettn }
    };

    return await _invoiceService.GetInvoiceStatusAsync(request);
}
```

### Fatura İndirme

```csharp
// PDF olarak indir
public async Task<byte[]> DownloadInvoicePdfAsync(string ettn)
{
    return await _invoiceService.DownloadInvoicePdfAsync(ettn);
}

// HTML olarak indir
public async Task<string> DownloadInvoiceHtmlAsync(string ettn)
{
    return await _invoiceService.DownloadInvoiceHtmlAsync(ettn);
}
```

### Fatura GİB'e Gönderme

```csharp
public async Task<bool> SendToGibAsync(string ettn)
{
    return await _invoiceService.SendInvoiceToGibAsync(ettn);
}
```

### E-Arşiv Fatura İptali

```csharp
public async Task<bool> CancelInvoiceAsync(string ettn)
{
    return await _invoiceService.CancelInvoiceAsync(ettn, "Müşteri talebi");
}
```

## 🔐 Kimlik Doğrulama

Token yönetimi otomatik olarak yapılır:
- İlk istek sırasında otomatik login
- Token süresi dolmadan önce otomatik yenileme
- Thread-safe token cache

Manuel token yönetimi gerekmez, servisler otomatik olarak geçerli token'ı kullanır.

## ⚙️ Konfigürasyon Parametreleri

| Parametre | Açıklama | Varsayılan |
|-----------|----------|------------|
| `BaseUrl` | API base URL | - |
| `Username` | Entegrasyon kullanıcı adı | - |
| `Password` | Entegrasyon şifresi | - |
| `TokenExpirationMinutes` | Token geçerlilik süresi (dakika) | 60 |
| `TimeoutSeconds` | HTTP request timeout (saniye) | 30 |
| `MaxRetryAttempts` | Maksimum retry sayısı | 3 |
| `IsTestEnvironment` | Test ortamı mı? | true |

## 🧪 Test Ortamı

Test ortamında kullanabileceğiniz hazır kullanıcı:
- Kullanıcı Adı: `OdaksoftWS`
- Şifre: `OdaksoftWS`
- Portal: https://portal-test.odaksoft.com.tr

## 📚 API Dokümantasyonu

Detaylı API dokümantasyonu için `swagger/swagger.json` dosyasına bakınız.

## 🔧 Hata Yönetimi

Tüm servisler exception handling içerir:
- HTTP hataları otomatik retry ile yönetilir
- Validasyon hataları anlamlı mesajlarla döner
- Tüm hatalar loglama ile kaydedilir

## 📝 Validasyon Kuralları

### Fatura Oluşturma
- Fatura tipi ve senaryosu zorunlu
- Fatura numarası maksimum 50 karakter
- Fatura tarihi gelecek tarih olamaz
- Para birimi 3 karakter (TRY, USD, EUR)
- En az bir fatura kalemi olmalı

### Müşteri Bilgileri
- VKN 10 haneli veya TC kimlik 11 haneli
- Müşteri adı maksimum 200 karakter
- Adres maksimum 500 karakter
- E-posta formatı geçerli olmalı

### Fatura Kalemleri
- Ürün/hizmet adı maksimum 300 karakter
- Miktar 0'dan büyük olmalı
- KDV oranı 0-100 arası
- İskonto oranı 0-100 arası

## 🤝 Destek

Sorularınız için:
- Odaksoft Müşteri Hizmetleri
- API Dokümantasyonu: swagger.json

## 📄 Lisans

Bu proje ParPazar e-ticaret platformu için geliştirilmiştir.
