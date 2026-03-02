using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.ProductDto
{
    [AutoMap(typeof(Product), ReverseMap = true)]
    public class ProductUpsertDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; } = null!;
        public string? ShortName { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public decimal? CartMinValue { get; set; } = 1;
        public decimal? CartMaxValue { get; set; }
        public decimal? Weight { get; set; } = 0;
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public decimal? Height { get; set; }
        public decimal? Price { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? RetailPrice { get; set; }
        public string? Gtin { get; set; }
        public bool IsNewsProduct{get;set;} = false;


        public string ? WebKeyword{get;set;}

        public DateTime CreatedDate { get; set; }

        public int? BrandId { get; set; }
        public int? ProductTypeId { get; set; }
        public int? TaxId { get; set; }
        public string ? VideoUrl{get;set;}
        public string? DocumentUrl {get;set;}
        public string ? DocumentUrl2{get;set;}
        public int Status { get; set; }
        public int? CompanyId{get;set;}
        
        public decimal ? MinPrice{get;set;} 
        public decimal ? MaxPrice{get;set;} 
        public decimal ? AvgPrice{get;set;} 
        public int ? AdvertCount{get;set;} 
        public bool IsGift{get;set;}
        public bool IsStockFollow { get; set; } = true;

        [Ignore]
        public bool StatusBool{ get; set; }
        public List<int>? CategoryIds { get; set; }
        public int? UnitId { get; set; }
        public List<int>? UnitIds { get; set; }
    }
 
}
