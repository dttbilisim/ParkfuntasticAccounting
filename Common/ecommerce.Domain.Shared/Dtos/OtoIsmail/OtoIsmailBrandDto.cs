namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailBrandDto
{
    public string? Kod { get; set; }
    public string? Aciklama { get; set; }
}

public class OtoIsmailResultBrandsDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailBrandDto>? Data { get; set; }
}
