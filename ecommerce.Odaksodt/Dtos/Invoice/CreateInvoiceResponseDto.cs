using System.Text.Json.Serialization;

namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Odaksoft API fatura oluşturma response DTO
/// API Response: {"errorList":null,"data":[{"ettn":"...","documentNo":"...","refNo":"...","status":true/false,"message":"..."}],"status":true,"message":"...","exceptionMessage":null}
/// </summary>
public class CreateInvoiceResponseDto
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }

    [JsonPropertyName("errorList")]
    public List<string>? ErrorList { get; set; }

    [JsonPropertyName("data")]
    public List<CreateInvoiceDataItemDto>? Data { get; set; }

    // Yardımcı property'ler (JSON'dan gelmez, kod tarafında kullanılır)
    [JsonIgnore]
    public bool Success => Status && Data != null && Data.Any(d => d.Status);

    [JsonIgnore]
    public string? Ettn => Data?.FirstOrDefault(d => d.Status)?.Ettn;

    [JsonIgnore]
    public string? ErrorMessage => Data?.FirstOrDefault(d => !d.Status)?.Message 
        ?? Message 
        ?? ExceptionMessage;
}

/// <summary>
/// Fatura data item - her fatura için ayrı sonuç
/// </summary>
public class CreateInvoiceDataItemDto
{
    [JsonPropertyName("ettn")]
    public string? Ettn { get; set; }

    [JsonPropertyName("documentNo")]
    public string? DocumentNo { get; set; }

    [JsonPropertyName("refNo")]
    public string? RefNo { get; set; }

    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }
}
