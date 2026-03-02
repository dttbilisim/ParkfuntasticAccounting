using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.ExpenseDto
{
    public class ExpenseDefinitionUpsertDto
    {
        public int? Id { get; set; }
        public ExpenseOperationType OperationType { get; set; } = ExpenseOperationType.Gider;
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }
}


