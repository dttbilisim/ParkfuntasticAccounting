namespace ecommerce.Domain.Shared.Dtos.Bank.BankParameterDto
{
    public class BankParameterListDto
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string BankName { get; set; }
    }
}
