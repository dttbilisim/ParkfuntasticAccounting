namespace ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardInstallmentDto
{
    public class BankCreditCardInstallmentListDto
    {
        public int Id { get; set; }
        public int CreditCardId { get; set; }
        public int Installment { get; set; }
        public decimal InstallmentRate { get; set; }
        public bool Active { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string CreditCardName { get; set; }
    }
}
