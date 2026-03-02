namespace ecommerce.Cargo.Mng.Models;

public class OrderPiece
{
    public string Barcode { get; set; } = null!;

    public int Desi { get; set; }

    public int Kg { get; set; }

    public string Content { get; set; } = null!;
}