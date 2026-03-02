using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.CheckDto
{
    public class CheckListDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int BankId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public int? BankBranchId { get; set; }
        public string? BankBranchName { get; set; }
        public string? CityName { get; set; }
        public string? TownName { get; set; }
        public string CheckNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public CheckStatus CheckStatus { get; set; }
        public string CheckStatusName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BranchId { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public DateTime? SettlementDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
