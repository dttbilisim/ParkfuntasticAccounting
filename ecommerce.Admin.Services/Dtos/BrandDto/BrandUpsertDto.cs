using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.BrandDto
{
    [AutoMap(typeof(Brand), ReverseMap = true)]
    public class BrandUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; }
        public int? BranchId { get; set; }
        public string? ImageUrl { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
