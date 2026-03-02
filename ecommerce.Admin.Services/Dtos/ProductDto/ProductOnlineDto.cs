using AutoMapper;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ProductDto;
[AutoMap(typeof(ProductOnline))]
public class ProductOnlineDto{
    public int Id{get;set;}
    public int ProductId{get;set;}
    public string ? Name{get;set;}
    public string ? ImageUrl{get;set;}
    public string ? Brand{get;set;}
    public string ?Barcodes{get;set;}
    public decimal ? Psf{get;set;} = 0;
    public decimal ? MinPrice{get;set;} = 0;
    public decimal ? MaxPrice{get;set;} = 0;
    public DateTime Created{get;set;}=DateTime.Now;
    public DateTime? Updated{get;set;}
}
