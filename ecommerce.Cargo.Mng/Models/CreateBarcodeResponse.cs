namespace ecommerce.Cargo.Mng.Models;

public class CreateBarcodeResponse
{
    public string ReferenceId { get; set; } = null!;

    public string InvoiceId { get; set; } = null!;

    public string ShipmentId { get; set; } = null!;

    public List<Barcode> Barcodes { get; set; } = new();

    public class Barcode
    {
        public int PieceNumber { get; set; }

        public string Value { get; set; } = null!;
    }
}