using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities{
    public class OrderItems : AuditableEntity<int>{
        public int OrderId{get;set;}
        public int BrandId{get;set;}
        public int ProductId{get;set;}
        public string ? ProductName{get;set;}
        public int Quantity{get;set;}
        public int Stock{get;set;}
        public decimal OldPrice{get;set;}
        public decimal Price{get;set;}
        public decimal TotalPrice{get;set;}
        public int ? CommissionRateId{get;set;} = 0;
        public int ? CommissionRatePercent{get;set;} = 0;
        public decimal ? CommissionTotal{get;set;} = 0;
        public decimal ? DiscountAmount{get;set;} = 0;
        public DateTime ExprationDate{get;set;}
        public decimal MerhantCommision{get;set;} = 0;
        public decimal SubmerhantCommision{get;set;} = 0;
        public string PaymentTransactionId{get;set;}
        public decimal Width{get;set;} = 0;
        public decimal Length{get;set;} = 0;
        public decimal Height{get;set;} = 0;
        public decimal CargoDesi{get;set;} = 0;
        
        // Item-level cargo tracking (NEW - for warehouse-based shipping)
        public string? CargoExternalId { get; set; }
        public string? CargoTrackNumber { get; set; }
        public string? CargoTrackUrl { get; set; }
        public bool? CargoRequestHandled { get; set; }
        public DateTime? ShipmentDate { get; set; }
        
        public virtual List<ProductImage> ProductImages{get;set;}
        [JsonIgnore]
        [ForeignKey("OrderId")] public Orders Orders{get;set;}
        [ForeignKey("ProductId")] public Product Product{get;set;}
        // BrandId FK removed - kept as simple column without FK constraint
        // Navigation property removed to prevent EF from creating FK
        public string? SourceId { get; set; }
        public bool IsSellerBasketStatus { get; set; } = false;
        public bool IsSellerOrderStatus { get; set; } = false;
        public string? SellerOrderResult { get; set; }
        public virtual List<ProductCategories> ProductCategories{get;set;}
        public ICollection<OrderAppliedDiscount> AppliedDiscounts{get;set;} = new List<OrderAppliedDiscount>();
    }
}
