namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailStockInfoDto
{
    public string? Kod { get; set; }
    public string? Miktar { get; set; }
    public int? StokSayisi { get; set; }
    public int? Plaza { get; set; }
    public int? Gebze { get; set; }
    public int? Ankara { get; set; }
    public int? Ikitelli { get; set; }
    public int? Izmir { get; set; }
    public int? Samsun { get; set; }
    public int? Depo1030 { get; set; }
    public int? Depo13 { get; set; }
}

public class OtoIsmailResultStockByCodeDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailStockInfoDto>? Data { get; set; }
}

public class OtoIsmailResultStockIdDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailStockInfoDto>? Data { get; set; }
}

public class OtoIsmailResultStockByDateDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailStockInfoDto>? Data { get; set; }
}
