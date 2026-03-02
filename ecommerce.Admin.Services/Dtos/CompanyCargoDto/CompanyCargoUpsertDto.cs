using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.CompanyCargoDto
{
    [AutoMap(typeof(CompanyCargo), ReverseMap = true)]
    public class CompanyCargoUpsertDto
    {
        public int? Id { get; set; }
        public int? CargoId { get; set; }
        public int SellerId { get; set; }
        public decimal? MinBasketAmount { get; set; }

        public bool IsDefault { get; set; } = false;

        [Ignore]
        public bool StatusBool { get; set; } = true;
    }
}
