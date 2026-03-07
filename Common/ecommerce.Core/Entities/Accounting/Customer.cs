using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Hierarchical;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities.Accounting
{
    public class Customer : AuditableEntity<int>
    {
        public int CorporationId { get; set; }
        [ForeignKey(nameof(CorporationId))]
        public virtual Corporation Corporation { get; set; }

        public int? BranchId { get; set; }
        [ForeignKey(nameof(BranchId))]
        public virtual Branch? Branch { get; set; }
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public CustomerType Type { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? Fax { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }
        
        [MaxLength(500)]
        public string? OtherAddress { get; set; }

        [MaxLength(100)]
        public string? TaxOffice { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        public int? RegionId { get; set; }
        [ForeignKey("RegionId")]
        public virtual Region? Region { get; set; }

        public int? CityId { get; set; }
        [ForeignKey("CityId")]
        public virtual City? City { get; set; }

        public int? TownId { get; set; }
        [ForeignKey("TownId")]
        public virtual Town? Town { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RiskLimit { get; set; } = 0;

        public bool AllowCashSale { get; set; } = true;
        
        public int Vade { get; set; }

        public CustomerWorkingTypeEnum CustomerWorkingType { get; set; } = CustomerWorkingTypeEnum.Pesin;

        public bool TransferNewYear { get; set; } = true;

        /// <summary>PcPos transfer: Cari POS'ta aktif mi?</summary>
        public bool IsPcPos { get; set; }
        /// <summary>PcPos transfer: Kredi verilebilir mi?</summary>
        public bool IsCredit { get; set; }
        /// <summary>PcPos transfer: KDV hariç mi?</summary>
        public bool IsVatExcluded { get; set; }
        /// <summary>PcPos transfer: Güncel fiyatlar güncellenebilir mi?</summary>
        public bool IsCurrentPricesUpdatable { get; set; }
        /// <summary>PcPos transfer: Sokak acentası mı?</summary>
        public bool IsStreetAgency { get; set; }
        /// <summary>PcPos transfer: Şirket kodu (filtreleme için)</summary>
        [MaxLength(50)]
        public string? CompanyCode { get; set; }

        public virtual ICollection<CustomerBranch> CustomerBranches { get; set; } = new List<CustomerBranch>();
    }

    public enum CustomerType
    {
        Buyer = 1,          // Alıcı
        Seller = 2,         // Satıcı
        BuyerSeller = 3,    // Alıcı + Satıcı
        Other = 4,          // Diğer
        Employee = 5        // Personel
    }
}
