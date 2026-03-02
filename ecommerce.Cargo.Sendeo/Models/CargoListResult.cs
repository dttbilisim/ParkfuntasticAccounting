namespace ecommerce.Cargo.Sendeo.Models;

public class CargoListResult
{
    public bool Result { get; set; }

    public string? Message { get; set; }

    public int TotalCount { get; set; }

    public List<CargoListItem> CargoList { get; set; } = new();
}