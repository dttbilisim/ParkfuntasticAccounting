using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Identity;
namespace ecommerce.Core.Entities{
    public class Orders : AuditableEntity<int>{
        public OrderStatusType OrderStatusType{get;set;}
        public OrderPlatformType PlatformType{get;set;} = OrderPlatformType.Marketplace; // Default: Pazaryeri
        public string OrderNumber{get;set;}
        public int CompanyId{get;set;}
        public int SellerId{get;set;}
        public PaymentType PaymentTypeId{get;set;}
        public decimal ? DiscountTotal{get;set;} = 0;
        public int CargoId{get;set;}
        public decimal CargoPrice{get;set;}

        /// <summary>Kuryem Olur musun: Atanmış kurye (kurye teslimatı seçildiyse).</summary>
        public int? CourierId { get; set; }
        [ForeignKey(nameof(CourierId))]
        public Courier? Courier { get; set; }
        /// <summary>Hangi araçla teslim edilecek / teslim edildi. Sipariş kabul edildiğinde set edilir; liste ekranında şoför adı/plaka için kullanılır.</summary>
        public int? CourierVehicleId { get; set; }
        [ForeignKey(nameof(CourierVehicleId))]
        public CourierVehicle? CourierVehicle { get; set; }
        /// <summary>Kurye teslimat durumu (Assigned, Accepted, PickedUp, OnTheWay, Delivered, Cancelled).</summary>
        public CourierDeliveryStatus? CourierDeliveryStatus { get; set; }
        /// <summary>Teslimat tipi: Kargo vs Kurye (sepet seçimine göre).</summary>
        public DeliveryOptionType? DeliveryOptionType { get; set; }
        /// <summary>Kurye ile tahmini teslimat süresi (dakika).</summary>
        public int? EstimatedCourierDeliveryMinutes { get; set; }
        
        /// <summary>
        /// Şube ID (Multi-branch filtering)
        /// </summary>
        public int? BranchId { get; set; }

        /// <summary>
        /// PcPos transfer batch ID - Bu siparişin hangi transfer batch'inden geldiğini tutar.
        /// </summary>
        public int? OrderTransferId { get; set; }

        
        [Obsolete("Use OrderItems.CargoTrackNumber instead - cargo is now tracked per item")]
        public string ? CargoTrackNumber{get;set;}
        
        [Obsolete("Use OrderItems.CargoTrackUrl instead - cargo is now tracked per item")]
        public string ? CargoTrackUrl{get;set;}
        
        [Obsolete("Use OrderItems.CargoExternalId instead - cargo is now tracked per item")]
        public string ? CargoExternalId{get;set;}
        
        [Obsolete("Use OrderItems.CargoRequestHandled instead - cargo is now tracked per item")]
        public bool? CargoRequestHandled{get;set;}
        
        [Obsolete("Use OrderItems.ShipmentDate instead - cargo is now tracked per item")]
        public DateTime ? ShipmentDate{get;set;}
        
        public DateTime ? DeliveryDate{get;set;}
        public string ? DeliveryTo{get;set;}
        public decimal ProductTotal{get;set;}
        public decimal OrderTotal{get;set;}
        public decimal GrandTotal{get;set;}
        public int SubmerhantCommissionRate{get;set;} = 0;
        public decimal SubmerhantCommisionTotal{get;set;} = 0;
        public decimal MerhanCommission{get;set;} = 0;
        public decimal IyzicoPaidTotal{get;set;} = 0;

        //iyzico bilgileri
        public string ? PaymentToken{get;set;}
        public string ? CardAssociation{get;set;}
        public string ? CardFamily{get;set;}
        public string ? CardType{get;set;}
        public string ? CardBinNumber{get;set;}
        public int ? Installment{get;set;} = 0;
        public bool PaymentStatus{get;set;} = false;
        public bool IyzicoSellerTransferStatus{get;set;} = false;
        public DateTime? IyzicoSellerTransferDate{get;set;}
        
        //siparislerimde iade ve iptallerde kullanicilacak alanlar.
        public int ? ReturnOrCancelMainId{get;set;}
        public int ? ReturnOrCancelSubId{get;set;}
        public string ? ReturnOrCancelDescription{get;set;}
        public DateTime? ReturnOrCancelDate{get;set;}
        public string ? ReturnOrCancelImagePatth{get;set;}
        public string ? ReturnOrCancelAdminDescription {get;set;}
        public OrderProblemStatus? ProblemStatus { get; set; }
        public string? ReturnCargoReference { get; set; }
        public string? ReturnCargoTrackNumber { get; set; }
        public string? ReturnCargoTrackUrl { get; set; }
        public string? ReturnCargoExternalId { get; set; }
        public DateTime? ReturnShipmentDate { get; set; }
        public DateTime? ReturnDeliveryDate { get; set; }
        public bool IsSellerApproved{get;set;}
        
        
        //Tamamlanan siparis onay tarihi gerekli
        public DateTime? OrderCompletedDate{get;set;}
        
        //Fatura yuklemesi icin Fatura dosya url

        public string ? InvoicePath{get;set;}

        public string? PaymentId { get; set; }

        public string? IyzicoCanceledMessage { get; set; }
        public DateTime? IyzicoCancelDate { get; set; }

        /// <summary>Paket ürünler için voucher kodu (ParkFuntastic uyarlaması).</summary>
        public string? Voucher { get; set; }
        /// <summary>Paket ürünler için rehber/acenta ismi (ParkFuntastic uyarlaması).</summary>
        public string? GuideName { get; set; }

        public virtual List<OrderItems> OrderItems{get;set;}

        // Web context için User (CompanyId FK ile)
        [ForeignKey("CompanyId")] 
        public User? User{get;set;}
        
        // Admin context için ApplicationUser (CompanyId FK ile)
        [ForeignKey("CompanyId")] 
        public ApplicationUser? ApplicationUser{get;set;}
        
        // Helper property: Context'e göre doğru user'ı döner
        [NotMapped]
        public IdentityUser<int>? CurrentUser => ApplicationUser ?? (IdentityUser<int>?)User;
        
        [NotMapped]
        public string? UserEmail => ApplicationUser?.Email ?? User?.Email;
        
        [NotMapped]
        public string? UserFullName => ApplicationUser?.FullName ?? User?.FullName;
        
        [NotMapped]
        public string? UserPhoneNumber => ApplicationUser?.PhoneNumber ?? User?.PhoneNumber;
        
        [ForeignKey("SellerId")] public Seller Seller{get;set;}
        [ForeignKey("UserAddressId")] public UserAddress ? UserAddress{get;set;}
        public int? UserAddressId{get;set;}
        
        public int? BankCardId{get;set;}
        [ForeignKey("BankCardId")] public BankCard? BankCard{get;set;}
        
        public int? BankId{get;set;}
        [ForeignKey("BankId")] public Bank ? Bank{get;set;}
        
        [ForeignKey("CargoId")] public Cargo ? Cargo{get;set;}
        
        /// <summary>
        /// B2B: Cari ID (plasiyer adına sipariş oluşturulduğunda — sipariş listesinde görünmesi için)
        /// </summary>
        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual ecommerce.Core.Entities.Accounting.Customer? Customer { get; set; }

        /// <summary>
        /// Fatura ID (hangi faturaya dönüştüğünü tutmak için)
        /// </summary>
        public int? InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public virtual ecommerce.Core.Entities.Accounting.Invoice? Invoice { get; set; }
        
        public ICollection<OrderAppliedDiscount> AppliedDiscounts { get; set; } = new List<OrderAppliedDiscount>();
    }
}
