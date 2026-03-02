using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.ExpenseDto
{
    public class ExpenseDefinitionListDto
    {
        public int Id { get; set; }
        public ExpenseOperationType OperationType { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
    }
}


