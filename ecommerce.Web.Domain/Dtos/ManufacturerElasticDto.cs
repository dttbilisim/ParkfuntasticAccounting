namespace ecommerce.Web.Domain.Dtos;

public class ManufacturerElasticDto
{
    public int Id { get; set; }
    public string DatKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int VehicleType { get; set; }
    public string? LogoUrl { get; set; }
    public int Order { get; set; }
    public int ModelCount { get; set; }
    public List<BaseModelDto> Models { get; set; } = new();
}

public class BaseModelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int VehicleType { get; set; }
    public string ManufacturerKey { get; set; } = string.Empty;
    public string?ManufacturerName { get; set; }
    public string BaseModelKey { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<SubModelDto> SubModels { get; set; } = new();
}

public class SubModelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubModelKey { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

