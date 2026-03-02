using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.PharmacyTypeDto
{
    [AutoMap(typeof(PharmacyType))]
    public class PharmacyTypeListDto
    {

        public int? Id { get; set; }
        public string Name { get; set; }

        public int Status { get; set; }

        [Ignore]
        public string StatusStr
        {
            get
            {
                switch (Status)
                {
                    case 0: return "Pasif";
                    case 1: return "Aktif";
                    case 99: return "Silinmi?";
                    default: return "Belirsiz";
                };
            }
        }
    }
}
