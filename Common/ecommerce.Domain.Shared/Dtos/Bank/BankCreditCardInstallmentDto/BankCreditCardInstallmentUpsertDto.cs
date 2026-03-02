namespace ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardInstallmentDto
{
    public class BankCreditCardInstallmentUpsertDto
    {
        public int Id { get; set; }
        public int CreditCardId { get; set; }
        public int Installment { get; set; }
        public decimal InstallmentRate { get; set; }
        public bool Active { get; set; }
        public bool Deleted { get; set; }
    }
}
