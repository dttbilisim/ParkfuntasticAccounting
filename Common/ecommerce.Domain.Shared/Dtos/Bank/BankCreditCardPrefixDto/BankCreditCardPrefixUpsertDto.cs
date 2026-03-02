namespace ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardPrefixDto
{
    public class BankCreditCardPrefixUpsertDto
    {
        public int Id { get; set; }
        public int CreditCardId { get; set; }
        public string Prefix { get; set; }
        public bool Active { get; set; }
        public bool Deleted { get; set; }
    }
}
