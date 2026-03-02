using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.TierDto
{
    [AutoMap(typeof(Tier), ReverseMap = true)]
    public class TierUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = default!;
        public int? BranchId { get; set; }
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
