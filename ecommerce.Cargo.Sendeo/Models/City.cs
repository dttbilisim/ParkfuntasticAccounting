namespace ecommerce.Cargo.Sendeo.Models;

public class City
{
    public string CityName { get; set; } = null!;

    public int CityId { get; set; }

    public List<District> Districts { get; set; } = new();
}