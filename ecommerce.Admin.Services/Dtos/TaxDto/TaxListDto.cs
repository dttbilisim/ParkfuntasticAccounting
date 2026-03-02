using AutoMapper;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.TaxDto
{
    [AutoMap(typeof(Tax))]
    public class TaxListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int TaxRate { get; set; }
    }
}
