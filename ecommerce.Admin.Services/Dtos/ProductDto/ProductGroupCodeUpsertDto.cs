using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ProductDto;
[AutoMap(typeof(ProductGroupCode), ReverseMap = true)]
public class ProductGroupCodeUpsertDto{
    public int? Id { get; set; }
    public int ProductId{get;set;}
    public string OemCode { get; set; }
    public int Status { get; set; }

    [Ignore]
    public bool StatusBool { get; set; }
}
