
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities.Warehouse;

namespace ecommerce.Admin.Domain.Dtos.WarehouseShelfDto
{
    public class WarehouseShelfBatchCreateDto
    {
        public int WarehouseId { get; set; }
        public string Prefix { get; set; } // e.g., "A-"
        public int StartNumber { get; set; } // e.g., 1
        public int EndNumber { get; set; } // e.g., 100
        public string Suffix { get; set; } // e.g., ""
        public string Description { get; set; }
        
        [Ignore]
        public bool StatusBool { get; set; } = true;
    }
}
