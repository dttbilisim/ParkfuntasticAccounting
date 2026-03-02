namespace ecommerce.EP.Configuration;

/// <summary>
/// Mobil API (EP) şube ayarı. Global query filter'da kullanılır; farklı şirketlerin verisi karışmasın diye
/// her EP instance tek bir BranchId ile çalışır (appsettings "Branch:BranchId").
/// </summary>
public class EpBranchOptions
{
    public const string SectionName = "Branch";

    /// <summary>Bu API instance'ının bağlı olduğu şube ID. 0 ise tenant filtresi uygulanmaz (tüm veri).</summary>
    public int BranchId { get; set; } = 1;
}
