namespace ecommerce.Domain.Shared.Dtos.Bank.BankParameterDto
{
    public class BankParameterUpsertDto
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
