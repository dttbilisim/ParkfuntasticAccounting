using AutoMapper;
using ecommerce.Core.Entities.Hierarchical;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.HierarchicalDto
{
    [AutoMap(typeof(Branch), ReverseMap = true)]
    public class BranchListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public int CorporationId { get; set; }
        public string CorporationName { get; set; } = null!;
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public int? TownId { get; set; }
        public string? TownName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    [AutoMap(typeof(Branch), ReverseMap = true)]
    public class BranchUpsertDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Şube adı zorunludur")]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Şube kodu zorunludur")]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Şirket seçimi zorunludur")]
        public int CorporationId { get; set; }

        public int? CityId { get; set; }
        public int? TownId { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
