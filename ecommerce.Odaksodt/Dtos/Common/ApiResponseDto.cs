namespace ecommerce.Odaksodt.Dtos.Common;

/// <summary>
/// Genel API response DTO
/// </summary>
/// <typeparam name="T">Response data tipi</typeparam>
public class ApiResponseDto<T>
{
    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }
}
