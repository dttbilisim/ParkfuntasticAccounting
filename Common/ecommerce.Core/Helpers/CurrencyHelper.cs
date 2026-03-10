namespace ecommerce.Core.Helpers;

/// <summary>
/// Para birimi formatlama yardımcısı. Tutarları tutarlı şekilde gösterir.
/// </summary>
public static class CurrencyHelper
{
    /// <summary>
    /// Para birimi koduna göre sembol döner. TRY/TL → ₺, EUR → €, USD → $
    /// </summary>
    public static string GetSymbol(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return "₺";
        var code = currencyCode.Trim().ToUpperInvariant();
        return code switch
        {
            "TRY" or "TL" or "TRL" => "₺",
            "EUR" => "€",
            "USD" => "$",
            "GBP" => "£",
            _ => code
        };
    }

    /// <summary>
    /// Tutarı formatlar: "1.234,56 ₺" formatında. currencyCode null ise varsayılan ₺ kullanılır.
    /// </summary>
    public static string FormatPrice(decimal amount, string? currencyCode = null)
    {
        var symbol = GetSymbol(currencyCode);
        return $"{amount:N2} {symbol}";
    }

    /// <summary>
    /// Tutarı formatlar: "₺1.234,56" formatında (sembol önde). currencyCode null ise varsayılan ₺ kullanılır.
    /// </summary>
    public static string FormatPricePrefix(decimal amount, string? currencyCode = null)
    {
        var symbol = GetSymbol(currencyCode);
        return $"{symbol}{amount:N2}";
    }
}
