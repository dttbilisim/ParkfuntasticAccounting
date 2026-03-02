using AutoMapper;
using ecommerce.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.SalesPersonDto
{
    [AutoMap(typeof(SalesPersonBranch), ReverseMap = true)]
    public class SalesPersonBranchUpsertDto
    {
        public int? Id { get; set; }
        public int SalesPersonId { get; set; }
        
        [Required(ErrorMessage = "Şube seçimi zorunludur")]
        public int BranchId { get; set; }
        
        public string? BranchName { get; set; }
        public int CorporationId { get; set; }
        public string? CorporationName { get; set; }
        public bool IsDefault { get; set; }
    }
}
