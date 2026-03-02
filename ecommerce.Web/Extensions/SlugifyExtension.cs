using System.Text;
using System.Text.RegularExpressions;

namespace ecommerce.Web.Extensions
{
    public static class SlugifyExtension
    {
        public static string ToSlug(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Türkçe karakterleri değiştir
            text = text.Replace("ı", "i")
                      .Replace("ğ", "g")
                      .Replace("ü", "u")
                      .Replace("ş", "s")
                      .Replace("ö", "o")
                      .Replace("ç", "c")
                      .Replace("İ", "I")
                      .Replace("Ğ", "G")
                      .Replace("Ü", "U")
                      .Replace("Ş", "S")
                      .Replace("Ö", "O")
                      .Replace("Ç", "C");

            // Küçük harfe çevir
            text = text.ToLowerInvariant();

            // Alfanumerik olmayan karakterleri kaldır
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

            // Boşlukları tire ile değiştir
            text = Regex.Replace(text, @"\s+", "-");

            // Ardışık tireleri tek tireye dönüştür
            text = Regex.Replace(text, @"-+", "-");

            // Baştaki ve sondaki tireleri kaldır
            text = text.Trim('-');

            return text;
        }
    }
}