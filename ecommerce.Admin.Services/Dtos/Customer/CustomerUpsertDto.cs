using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.Customer
{
    public class CustomerUpsertDto
    {
        public int? Id { get; set; }

        public int CorporationId { get; set; }

        [Required(ErrorMessage = "Cari Kodu zorunludur.")]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Cari Adı zorunludur.")]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public CustomerType Type { get; set; } = CustomerType.Buyer;

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

        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public int? RegionId { get; set; }
        
        [MaxLength(100)]
        public string? District { get; set; }
        
        // Şehir ve ilçe adları (görüntüleme/e-fatura için)
        public string? CityName { get; set; }
        public string? TownName { get; set; }

        public bool IsActive { get; set; } = true;
        public decimal RiskLimit { get; set; }
        public int Vade { get; set; }
        public CustomerWorkingTypeEnum CustomerWorkingType { get; set; } = CustomerWorkingTypeEnum.Pesin;
        public bool AllowCashSale { get; set; } = true;
        public bool TransferNewYear { get; set; } = true;
        public List<CustomerBranchUpsertDto> Branches { get; set; } = new();
    }
}
