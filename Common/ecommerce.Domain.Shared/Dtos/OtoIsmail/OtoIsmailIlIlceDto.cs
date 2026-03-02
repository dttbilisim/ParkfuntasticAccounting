namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailIlIlceDto
{
    public string? IlKodu { get; set; }
    public string? IlAdi { get; set; }
    public string? IlceKodu { get; set; }
    public string? IlceAdi { get; set; }
}

public class OtoIsmailResultIlIlceListesiDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailIlIlceDto>? Data { get; set; }
}
