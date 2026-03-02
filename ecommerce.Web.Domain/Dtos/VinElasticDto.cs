using Nest;

namespace ecommerce.Web.Domain.Dtos;

/// <summary>
/// VIN Index'ten dönen araç bilgisi DTO'su
/// Python vincreate.py scripti ile oluşturulan vin_index yapısına uygun
/// </summary>
public class VinElasticDto
{
    /// <summary>
    /// WMI (World Manufacturer Identifier) - VIN'in ilk 3 karakteri
    /// Örnek: "WBA" (BMW), "WDB" (Mercedes-Benz)
    /// </summary>
    [PropertyName("wmi")]
    public string Wmi { get; set; } = string.Empty;

    /// <summary>
    /// Üretici DAT Key
    /// </summary>
    [PropertyName("manufacturer_key")]
    public string ManufacturerKey { get; set; } = string.Empty;

    /// <summary>
    /// Üretici Adı (Marka)
    /// Örnek: "BMW", "Mercedes-Benz", "Audi"
    /// </summary>
    [PropertyName("manufacturer_name")]
    public string ManufacturerName { get; set; } = string.Empty;

    /// <summary>
    /// Ana Model DAT Key
    /// </summary>
    [PropertyName("base_model_key")]
    public string BaseModelKey { get; set; } = string.Empty;

    /// <summary>
    /// Ana Model Adı
    /// Örnek: "3 Series (E90)", "C-Class (W204)"
    /// </summary>
    [PropertyName("base_model_name")]
    public string BaseModelName { get; set; } = string.Empty;

    /// <summary>
    /// Alt Model DAT Key (nullable)
    /// </summary>
    [PropertyName("sub_model_key")]
    public string? SubModelKey { get; set; }

    /// <summary>
    /// Alt Model Adı (nullable)
    /// Örnek: "320i", "C200 CDI"
    /// </summary>
    [PropertyName("sub_model_name")]
    public string? SubModelName { get; set; }

    /// <summary>
    /// Üretim Yılı Başlangıç
    /// </summary>
    [PropertyName("year_from")]
    public int YearFrom { get; set; }

    /// <summary>
    /// Üretim Yılı Bitiş
    /// </summary>
    [PropertyName("year_to")]
    public int YearTo { get; set; }

    /// <summary>
    /// VDS Code (Vehicle Descriptor Section - VIN'in 4-9 karakterleri)
    /// Örnek: "BA", "WBAA"
    /// </summary>
    [PropertyName("vds_code")]
    public string? VdsCode { get; set; }

    /// <summary>
    /// DOT E-Code
    /// </summary>
    [PropertyName("dot_ecode")]
    public string? DotEcode { get; set; }

    /// <summary>
    /// DAT Process Number
    /// </summary>
    [PropertyName("dat_process_number")]
    public List<string>? DatProcessNumber { get; set; }

    /// <summary>
    /// Arama için kullanılan anahtar kelimeler
    /// </summary>
    [PropertyName("search_keywords")]
    public string SearchKeywords { get; set; } = string.Empty;

    /// <summary>
    /// Bu araç için uyumlu OEM parça listesi
    /// </summary>
    [PropertyName("oem_parts")]
    public List<VinOemPartDto> OemParts { get; set; } = new();

    /// <summary>
    /// Kayıt oluşturulma tarihi
    /// </summary>
    [PropertyName("created_date")]
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// VIN Index'teki OEM parça bilgisi
/// </summary>
public class VinOemPartDto
{
    /// <summary>
    /// OEM Parça Numarası
    /// </summary>
    [PropertyName("oem")]
    public string Oem { get; set; } = string.Empty;

    /// <summary>
    /// Parça Adı/Açıklaması
    /// </summary>
    [PropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// VIN arama sonuç DTO'su
/// </summary>
public class VinSearchResultDto
{
    /// <summary>
    /// Bulunan toplam kayıt sayısı
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Bulunan araç kayıtları
    /// </summary>
    public List<VinElasticDto> Results { get; set; } = new();

    /// <summary>
    /// Arama başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// VIN decode sonuç DTO'su
/// </summary>
public class VinDecodeResultDto
{
    /// <summary>
    /// Girilen VIN numarası
    /// </summary>
    public string VinNumber { get; set; } = string.Empty;

    /// <summary>
    /// VIN geçerli mi?
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// WMI (İlk 3 karakter)
    /// </summary>
    public string? Wmi { get; set; }

    /// <summary>
    /// VDS (4-9 karakterler)
    /// </summary>
    public string? Vds { get; set; }

    /// <summary>
    /// Tespit edilen üretici
    /// </summary>
    public string? ManufacturerName { get; set; }

    /// <summary>
    /// Eşleşen araç kayıtları
    /// </summary>
    public List<VinElasticDto> MatchedVehicles { get; set; } = new();

    /// <summary>
    /// Decode başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
