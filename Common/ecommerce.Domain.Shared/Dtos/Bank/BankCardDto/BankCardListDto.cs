namespace ecommerce.Domain.Shared.Dtos.Bank.BankCardDto
{
    public class BankCardListDto
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public bool ManufacturerCard { get; set; }
        public bool CampaignCard { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string BankName { get; set; }
    }
}
