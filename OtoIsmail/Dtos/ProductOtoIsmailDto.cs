using ecommerce.Core.Dtos;
namespace OtoIsmail.Dtos;
public class ProductOtoIsmailDto
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
    public PriceDto? Fiyat1 { get; set; }
    public PriceDto? Fiyat2 { get; set; }
    public PriceDto? Fiyat3 { get; set; }
    public PriceDto? Fiyat4 { get; set; }
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
    public string? ParaBirimi { get; set; }
}
