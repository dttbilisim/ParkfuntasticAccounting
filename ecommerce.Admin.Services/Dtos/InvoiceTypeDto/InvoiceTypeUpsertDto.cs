using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.InvoiceTypeDto
{
    [AutoMap(typeof(InvoiceTypeDefinition), ReverseMap = true)]
    public class InvoiceTypeUpsertDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public int Status { get; set; }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}

