namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailOrderStatusDto
{
    public string? StokKodu { get; set; }
    public string? StokAdi { get; set; }
    public float Miktar { get; set; }
    public string? Durum { get; set; }
    public string? GonderimTarihi { get; set; }
    public string? OnayTarihi { get; set; }
    public DateTime? DepoOnayTarihi { get; set; }
    public DateTime? ToplamaBaslamaTarihi { get; set; }
    public DateTime? ToplamaBitisTarihi { get; set; }
    public DateTime? KontrolBaslamaTarihi { get; set; }
    public DateTime? FaturaTarihi { get; set; }
    public string? FaturaNo { get; set; }
    public string? KargoDurum { get; set; }
    public string? KargoTakipNo { get; set; }
}

public class OtoIsmailResultOrderStatusDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailOrderStatusDto>? Data { get; set; }
}
