using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.InvoiceTypeDto
{
    [AutoMap(typeof(InvoiceTypeDefinition))]
    public class InvoiceTypeListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public EntityStatus Status { get; set; }
    }
}

