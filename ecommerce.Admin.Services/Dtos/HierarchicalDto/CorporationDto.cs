using AutoMapper;
using ecommerce.Core.Entities.Hierarchical;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.HierarchicalDto
{
    [AutoMap(typeof(Corporation), ReverseMap = true)]
    public class CorporationListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    [AutoMap(typeof(Corporation), ReverseMap = true)]
    public class CorporationUpsertDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Şirket adı zorunludur")]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [MaxLength(100)]
        public string? TaxOffice { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
