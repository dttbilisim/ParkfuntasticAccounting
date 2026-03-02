namespace ecommerce.Cargo.Mng.Models;

public class CreateBarcodeRequest
{
    public string ReferenceId { get; set; } = null!;

    public string? BillOfLandingId { get; set; }

    public int IsCOD { get; set; }

    public double CodAmount { get; set; }

    public int PrintReferenceBarcodeOnError { get; set; }

    public string? Message { get; set; }

    public string? AdditionalContent1 { get; set; }

    public string? AdditionalContent2 { get; set; }

    public string? AdditionalContent3 { get; set; }

    public string? AdditionalContent4 { get; set; }

    public List<OrderPiece> OrderPieceList { get; set; } = new();
}