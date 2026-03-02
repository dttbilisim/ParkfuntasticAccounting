namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailProductDto
{
    public int NetsisStokId { get; set; }
    public string? Kod { get; set; }
    public string? OrjinalKod { get; set; }
    public string? Ad { get; set; }
    public string? Marka { get; set; }
    public string? Birim { get; set; }
    public string? GrupKodu { get; set; }
    public string? Barkod1 { get; set; }
    public string? Barkod2 { get; set; }
    public string? Barkod3 { get; set; }
    public string? ImageUrl { get; set; }
    public OtoIsmailPriceDto? Fiyat1 { get; set; }
    public OtoIsmailPriceDto? Fiyat2 { get; set; }
    public OtoIsmailPriceDto? Fiyat3 { get; set; }
    public OtoIsmailPriceDto? Fiyat4 { get; set; }
    public double KDV { get; set; }
    public string? Oem { get; set; }
    public double Pay { get; set; }
    public double Payda { get; set; }
    public int StokSayisi { get; set; }
    public int Plaza { get; set; }
    public int Gebze { get; set; }
    public int Ankara { get; set; }
    public decimal Nakliye { get; set; }
}

public class OtoIsmailPriceDto
{
    public string? ParaBirimi { get; set; }
    public double Deger { get; set; }
}

public class OtoIsmailResultProductsDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailProductDto>? Data { get; set; }
}
