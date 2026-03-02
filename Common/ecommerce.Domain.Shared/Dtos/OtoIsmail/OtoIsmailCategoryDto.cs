namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailCategoryDto
{
    public string? Kategori1 { get; set; }
    public string? Kategori2 { get; set; }
    public string? Kategori3 { get; set; }
    public string? Kategori4 { get; set; }
}

public class OtoIsmailResultCategoryDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailCategoryDto>? Data { get; set; }
}

public class OtoIsmailCategoryProductDto
{
    public int NetsisStokId { get; set; }
}

public class OtoIsmailResultCategoryProductDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailCategoryProductDto>? Data { get; set; }
}
