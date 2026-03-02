using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.PharmacyTypeDto
{
    [AutoMap(typeof(PharmacyType), ReverseMap = true)]
    public class PharmacyTypeUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
