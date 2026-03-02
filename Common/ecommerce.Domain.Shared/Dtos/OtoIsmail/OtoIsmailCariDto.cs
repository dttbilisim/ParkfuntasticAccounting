namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailCariDto
{
    public string? CariKodu { get; set; }
    public string? Unvan { get; set; }
    public string? VergiNumarasi { get; set; }
}

public class OtoIsmailResultCariListesiDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailCariDto>? Data { get; set; }
}
