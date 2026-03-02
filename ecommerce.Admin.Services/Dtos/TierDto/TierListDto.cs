using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.TierDto
{
    [AutoMap(typeof(Tier))]
    public class TierListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int? BranchId { get; set; }
        public EntityStatus Status { get; set; }
    }
}
