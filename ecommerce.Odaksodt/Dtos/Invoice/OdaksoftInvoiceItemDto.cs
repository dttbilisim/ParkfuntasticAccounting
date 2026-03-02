using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft fatura item DTO
/// </summary>
public class OdaksoftInvoiceItemDto
{
    /// <summary>
    /// Para birimi kodu
    /// </summary>
    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = "TRY";

    /// <summary>
    /// ETTN (Elektronik Transfer Tanımlama Numarası) - GUID formatında zorunlu
    /// </summary>
    [JsonPropertyName("ettn")]
    public string Ettn { get; set; } = string.Empty;

    /// <summary>
    /// Fatura detayları (kalemler)
    /// </summary>
    [JsonPropertyName("invoiceDetail")]
    public List<OdaksoftInvoiceDetailDto> InvoiceDetail { get; set; } = new();

    /// <summary>
    /// Fatura tipi (SATIS, IADE, vb.)
    /// </summary>
    [JsonPropertyName("invoiceType")]
    public string InvoiceType { get; set; } = string.Empty;

    /// <summary>
    /// API tarafından hesaplansın mı
    /// </summary>
    [JsonPropertyName("isCalculateByApi")]
    public bool IsCalculateByApi { get; set; } = true;

    /// <summary>
    /// Taslak mı
    /// </summary>
    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; set; } = false;

    /// <summary>
    /// Profil (TEMELFATURA, TICARIFATURA, EARSIVFATURA)
    /// </summary>
    [JsonPropertyName("profile")]
    public string Profile { get; set; } = string.Empty;

    /// <summary>
    /// Belge numarası (fatura numarası)
    /// </summary>
    [JsonPropertyName("docNo")]
    public string DocNo { get; set; } = string.Empty;

    /// <summary>
    /// Belge tarihi
    /// </summary>
    [JsonPropertyName("docDate")]
    public DateTime DocDate { get; set; }

    /// <summary>
    /// Döviz kuru
    /// </summary>
    [JsonPropertyName("currencyRate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? CurrencyRate { get; set; }

    /// <summary>
    /// Gönderen tipi
    /// </summary>
    [JsonPropertyName("senderType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SenderType { get; set; }

    /// <summary>
    /// Fatura hesap bilgileri (müşteri)
    /// </summary>
    [JsonPropertyName("invoiceAccount")]
    public OdaksoftInvoiceAccountDto InvoiceAccount { get; set; } = new();

    /// <summary>
    /// Notlar
    /// </summary>
    [JsonPropertyName("notes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Notes { get; set; }

    /// <summary>
    /// Referans numarası
    /// </summary>
    [JsonPropertyName("refNo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RefNo { get; set; }
}
