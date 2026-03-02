namespace ecommerce.Domain.Shared.Dtos.Bank.BankDto
{
    public class BankUpsertDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SystemName { get; set; }
        public int BankCode { get; set; }
        public string LogoPath { get; set; }
        public bool UseCommonPaymentPage { get; set; }
        public bool DefaultBank { get; set; }
        public bool Active { get; set; }
    }
}
