using ecommerce.Core.Entities;
using ecommerce.Core.Utils;


namespace ecommerce.Admin.Domain.Dtos.ProductDto
{
    //[AutoMap(typeof(Product))]
    public class ProductListDto
    {
        public int Id { get; set; }

        public string IdStr
        {
            get
            {
                return Id.ToString();
            }
        }


        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Barcode { get; set; }
        public string Gtin { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public EntityStatus Status { get; set; }
        public decimal? RetailPrice { get; set; }

        public int BrandId { get; set; }
        public int ProductTypeId { get; set; }
        public string ? VideoUrl{get;set;}
        public string? DocumentUrl {get;set;}
        public string ? DocumentUrl2{get;set;}
      
        public string ? WebKeyword{get;set;}
        public bool IsCustomerCreated{get;set;}
        public int? CompanyId{get;set;}

        public Brand Brand { get; set; }
        public ProductType ProductType { get; set; }
        
        public bool ProductsWithoutImage { get; set; }
        public bool ProductsWithoutCategory { get; set; }
        public int ProductsImageCount { get; set; }
        public int ProductAdvertCount{get;set;}
      
        public string ?  Category1{get;set;}
        public string ?  Category2{get;set;} 
        public string ?  Category3{get;set;}
        public string ? Form {get;set;}
        public int ? Kdv{get;set;}
        public decimal Weight { get; set; }
        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public decimal Height { get; set; }
        public decimal ? MinPrice{get;set;} 
        public decimal ? MaxPrice{get;set;} 
        public decimal ? AvgPrice{get;set;} 
        public int ? AdvertCount{get;set;} 
        public bool IsStockFollow { get; set; }
       

    }

    public class ProductListForProjectionDto
    {
        public int Id { get; set; }

        public string IdStr
        {
            get
            {
                return Id.ToString();
            }
        }

        public decimal? RetailPrice { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Barcode { get; set; }
        public string Gtin { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public EntityStatus Status { get; set; }
        public decimal Weight { get; set; }
        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public decimal Height { get; set; }

        public int BrandId { get; set; }
        public int ProductTypeId { get; set; }
        public string? VideoUrl { get; set; }
        public string? DocumentUrl { get; set; }
        public string? DocumentUrl2 { get; set; }

        public string? WebKeyword { get; set; }
        public bool IsCustomerCreated { get; set; }
        public int? CompanyId { get; set; }

        public Brand Brand { get; set; }
        public ProductType ProductType { get; set; }

        public bool ProductsWithoutImage { get; set; }
        public bool ProductsWithoutCategory { get; set; }
        public int ProductsImageCount { get; set; }
        public int ProductAdvertCount{get;set;}

        public string ?  Category1{get;set;}
        public string ?  Category2{get;set;} 
        public string ?  Category3{get;set;}
        public string ? Form {get;set;}
        public int ? Kdv{get;set;}
      
        public decimal ? MinPrice{get;set;} 
        public decimal ? MaxPrice{get;set;} 
        public decimal ? AvgPrice{get;set;} 
        public int ? AdvertCount{get;set;} 
        public bool IsStockFollow { get; set; }
      

    }
} 