using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.CompanyRateDto
{
    [AutoMap(typeof(CompanyRate), ReverseMap = true)]
    public class CompanyRateUpsertDto
    {
        public int? Id { get; set; }
        public int? CompanyId { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? TierId { get; set; }
        public int? Rate { get; set; }
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedId { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
