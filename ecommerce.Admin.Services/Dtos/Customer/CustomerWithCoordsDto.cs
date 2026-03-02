namespace ecommerce.Admin.Domain.Dtos.Customer
{
    /// <summary>
    /// Harita ekranı için cari bilgileri — mobil taraf adres bilgisinden geocoding yapacak
    /// </summary>
    public class CustomerWithCoordsDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? CityName { get; set; }
        public string? TownName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }
}
