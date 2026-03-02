using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace ecommerce.Core.Entities
{
    public class ProductSellerItem : AuditableEntity<int>
    {
        public int CompanyId { get; set; }
        public int ProductId { get; set; }
        public int? BrandId { get; set; }

        public DateTime ExprationDate { get; set; }
        public int Stock { get; set; }

     
        public decimal Price { get; set; }
        public decimal DiscountPrice{get;set;} = 0;
        public decimal RetailPrice { get; set; }

        
        public int MaxSellCount{get;set;} = 1;
       
        public int MinSellCount { get; set; } = 1;

        public decimal PurchasePrice { get; set; }

        public int? CountSameProduct { get; set; } = 0;
        public decimal? MinSameProductPrice { get; set; } = 0;
        public decimal? MaxSameProductPrice { get; set; } = 0;
        public decimal? AvgSameProductPrice { get; set; } = 0;


        public string? StockCode { get; set; }

        public string? Description { get; set; }
        public bool IsFeatured { get; set; }
        public string ? IntegrationId{get;set;}



        [ForeignKey("ProductId")]
        
        [JsonIgnore]
        public Product  Product { get; set; }

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }

        [ForeignKey("BrandId")]
        public Brand? Brand { get; set; }
        public virtual List<ProductImage> ProductImage { get; set; }
    


    }
}
