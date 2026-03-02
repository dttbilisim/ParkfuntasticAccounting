using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;


public class ProductOtoIsmail : AuditableEntity<int>
{

    public int NetsisStokId { get; set; }
    public string? Kod { get; set; }
    public string? OrjinalKod { get; set; }
    public string? Ad { get; set; }
    public string? Marka { get; set; }
    public string? MarkaFull { get; set; }
    public string? Birim { get; set; }
    public string? GrupKodu { get; set; }
    public string? Barkod1 { get; set; }
    public string? Barkod2 { get; set; }
    public string? Barkod3 { get; set; }
    public string? ImageUrl { get; set; }

    public decimal? Fiyat1 { get; set; }
    public string? ParaBirimi1 { get; set; } // Added for Fiyat1 currency
    public decimal? Fiyat2 { get; set; }
    public decimal? Fiyat3 { get; set; }
    public string? ParaBirimi3 { get; set; } // Added for Fiyat3 currency
    public decimal? Fiyat4 { get; set; }

    public decimal? KDV { get; set; }
    public string? Oem { get; set; }
    public decimal? Payda { get; set; }

    public int? StokSayisi { get; set; }
    public int? Plaza { get; set; }
    public int? Gebze { get; set; }
    public int? Ankara { get; set; }
    public int? Ikitelli { get; set; }
    public int? Izmir { get; set; }
    public int? Samsun { get; set; }

    public decimal? Nakliye { get; set; }
    public int? Depo1030 { get; set; }
    public int? Depo13 { get; set; }
    public string? ParaBirimi { get; set; } // Keeping original just in case, or maybe used as default? I'll keep it.
}
