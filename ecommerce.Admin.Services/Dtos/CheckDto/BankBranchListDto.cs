namespace ecommerce.Admin.Domain.Dtos.CheckDto
{
    /// <summary>Banka şubesi dropdown / liste için (il-ilçe master data)</summary>
    public class BankBranchListDto
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int? TownId { get; set; }
        public string? TownName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Address { get; set; }
        public bool Active { get; set; }
        /// <summary>Dropdown gösterimi: Şube adı — İl / İlçe</summary>
        public string DisplayText => $"{Name} — {CityName}" + (string.IsNullOrEmpty(TownName) ? "" : " / " + TownName);
    }
}
