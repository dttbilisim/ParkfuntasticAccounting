using Nest;
namespace ecommerce.Domain.Shared.Dtos.Category;

public class CategoryElasticDto
{
    [PropertyName("Id")]
    public int Id { get; set; }

    [PropertyName("Name")]
    public string Name { get; set; } = null!;

    [PropertyName("ParentId")]
    public int? ParentId { get; set; }

    [PropertyName("Status")]
    public int Status { get; set; }

    [PropertyName("CreatedId")]
    public int CreatedId { get; set; }

    [PropertyName("CreatedDate")]
    public DateTime CreatedDate { get; set; }

    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }

    [PropertyName("ModifiedDate")]
    public DateTime ? ModifiedDate { get; set; }

    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }

    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }

    [PropertyName("IsMainPage")]
    public bool IsMainPage { get; set; }

    [PropertyName("IsMainSlider")]
    public bool IsMainSlider { get; set; }

    [PropertyName("Order")]
    public int Order { get; set; }

    [PropertyName("Height")]
    public int? Height { get; set; }

    [PropertyName("SubCategoryCount")]
    public int? SubCategoryCount { get; set; }

    [PropertyName("ImageUrl")]
    public string? ImageUrl { get; set; }
}
