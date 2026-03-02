namespace ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardPrefixDto
{
    public class BankCreditCardPrefixListDto
    {
        public int Id { get; set; }
        public int CreditCardId { get; set; }
        public string Prefix { get; set; }
        public bool Active { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string CreditCardName { get; set; }
    }
}
