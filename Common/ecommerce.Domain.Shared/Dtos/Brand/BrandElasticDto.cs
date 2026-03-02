using Nest;
namespace ecommerce.Domain.Shared.Dtos.Brand;

public class BrandElasticDto
{
    [PropertyName("Id")] public int Id { get; set; }

    [PropertyName("Name")] public string? Name { get; set; }

    [PropertyName("Status")] public int Status { get; set; }

    [PropertyName("CreatedDate")] public DateTime CreatedDate { get; set; }

    [PropertyName("ModifiedDate")] public DateTime? ModifiedDate { get; set; }

    [PropertyName("CreatedId")] public int CreatedId { get; set; }

    [PropertyName("ModifiedId")] public int? ModifiedId { get; set; }

    [PropertyName("DeletedDate")] public DateTime? DeletedDate { get; set; }

    [PropertyName("DeletedId")] public int? DeletedId { get; set; }

    [PropertyName("ImageUrl")] public string? ImageUrl { get; set; }
}