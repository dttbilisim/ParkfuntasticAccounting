using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.PcPosDto
{
    [AutoMap(typeof(PcPosDefinition), ReverseMap = true)]
    public class PcPosUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }

        public int CorporationId { get; set; }
        public int BranchId { get; set; }
        public int WarehouseId { get; set; }
        public int? PaymentTypeId { get; set; }
    }
}

