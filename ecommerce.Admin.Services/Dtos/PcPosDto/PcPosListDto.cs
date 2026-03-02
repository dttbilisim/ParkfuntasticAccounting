using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.PcPosDto
{
    [AutoMap(typeof(PcPosDefinition))]
    public class PcPosListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public EntityStatus Status { get; set; }
        public int? PaymentTypeId { get; set; }
        public string? PaymentTypeName { get; set; }
    }
}

