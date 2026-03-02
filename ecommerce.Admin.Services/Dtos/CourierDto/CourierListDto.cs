using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CourierDto;

public class CourierListDto
{
    public int Id { get; set; }
    public int ApplicationUserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int ServiceAreaCount { get; set; }
}

public class CourierDetailDto
{
    public int Id { get; set; }
    public int ApplicationUserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public List<CourierServiceAreaListDto> ServiceAreas { get; set; } = new();
}
