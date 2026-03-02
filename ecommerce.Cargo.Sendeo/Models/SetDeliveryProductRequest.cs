namespace ecommerce.Cargo.Sendeo.Models;

public class SetDeliveryProductRequest
{
    public int Count { get; set; }

    public string? ProductCode { get; set; }

    public string? Description { get; set; }

    public int Deci { get; set; }

    public int Weigth { get; set; }

    public int DeciWeight { get; set; }

    public decimal Price { get; set; }
}