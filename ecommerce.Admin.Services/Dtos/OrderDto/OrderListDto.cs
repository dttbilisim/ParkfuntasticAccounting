    using AutoMapper;
    using ecommerce.Core.Entities;
    using ecommerce.Core.Entities.Authentication;
    using ecommerce.Core.Utils;
    namespace ecommerce.Admin.Domain.Dtos.OrderDto
    {
        [AutoMap(typeof(Orders))]
        public class OrderListDto
        {
            public int Id { get; set; }
            public OrderStatusType OrderStatusType { get; set; }
            public OrderPlatformType PlatformType { get; set; } = OrderPlatformType.Marketplace; // Default: Pazaryeri
            public string OrderNumber { get; set; }
            public int SellerId { get; set; }
            public PaymentType PaymentTypeId { get; set; }
            public decimal? DiscountTotal { get; set; } = 0;
            public int CargoId { get; set; }
            public decimal CargoPrice { get; set; }
            public string? CargoTrackNumber { get; set; }
            public string? CargoTrackUrl { get; set; }
            public string? CargoExternalId { get; set; }
            public DateTime? ShipmentDate { get; set; }
            public DateTime? DeliveryDate { get; set; }
            public string ? DeliveryTo{get;set;}
            public decimal ProductTotal { get; set; }
            public decimal IyzicoPaidTotal { get; set; } = 0;
            public decimal OrderTotal { get; set; }
            public decimal GrandTotal { get; set; }
            public string CustomerName { get; set; } // Real Customer (Cari)
        public string BuyerName { get; set; }    // UserAddress (Alıcı)
            public int? CustomerId { get; set; } // ApplicationUser.CustomerId for invoice creation
            public int CompanyId { get; set; } // FK: Works for both User.Id and ApplicationUser.Id
            public User? Company { get; set; } // Nullable: Admin context'te null, Web context'te User
            public Seller Seller { get; set; }
            public UserAddress? UserAddress { get; set; }
            public Core.Entities.Cargo? Cargo { get; set; }
            public Bank? Bank { get; set; }
            public List<OrderItems> OrderItems { get; set; }
            public bool PaymentStatus { get; set; } = false;
            public string? IyzicoCanceledMessage { get; set; }
            public DateTime? IyzicoCancelDate { get; set; }
            public DateTime CreatedDate{get;set;}
            public int? InvoiceId { get; set; } // Fatura ID
            public string? InvoiceNo { get; set; } // Fatura No (navigation property'den)
            
            /// <summary>
            /// Siparişe bağlı tüm faturalar (birden fazla fatura olabilir)
            /// </summary>
            public List<OrderLinkedInvoiceDto> LinkedInvoices { get; set; } = new();
            
            public string? CreatorName { get; set; }
            public bool IsCreatedByPlasiyer { get; set; }
            
            public string PaymentStatusResult
            {

                get
                {
                    if (PaymentStatus)
                        return "Ödendi";
                    else
                        return "Ödenmedi";
                }
            }
        }
    
        /// <summary>
        /// Siparişe bağlı fatura bilgisi (LinkedInvoices için)
        /// </summary>
        public class OrderLinkedInvoiceDto
        {
            public int InvoiceId { get; set; }
            public string InvoiceNo { get; set; } = "";
            public string? Ettn { get; set; }
            public bool IsEInvoice { get; set; }
            public bool IsEArchive { get; set; }
            public string? EInvoiceStatus { get; set; }
            public DateTime InvoiceDate { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal DiscountTotal { get; set; }
            public decimal VatTotal { get; set; }
            public decimal GeneralTotal { get; set; }
        }

        /// <summary>
        /// Ürün bazlı geçmiş alışveriş kaydı — ürün arama listesinde "geçmiş alışverişlerim" için.
        /// </summary>
        public class ProductPurchaseHistoryItemDto
        {
            public int OrderId { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
            public DateTime OrderCreatedDate { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public OrderStatusType OrderStatusType { get; set; }
            public string SellerName { get; set; } = string.Empty;
        }
    }
