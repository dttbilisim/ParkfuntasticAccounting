using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.MonthDto
{
    [AutoMap(typeof(Month))]
    public class MonthListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int MonthNumber { get; set; }
        public int Order { get; set; }
    }
}


