using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ProductDto;
[AutoMap(typeof(ProductGroupCode))]
public class ProductGroupCodeListDto{
    public int Id { get; set; }
    public string OemCode { get; set; }
    public EntityStatus Status { get; set; }
}
