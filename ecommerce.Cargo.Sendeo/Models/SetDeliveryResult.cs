namespace ecommerce.Cargo.Sendeo.Models
{
    public class SetDeliveryResult
    {
        public string TrackingNumber { get; set; } = null!;

        public string TrackingUrl { get; set; } = null!;

        public string? Barcode { get; set; }

        public string? BarcodeZpl { get; set; }

        public List<string>? BarcodeNumbers { get; set; }

        public List<string>? BarcodeZpls { get; set; }

        public string? BarcodeEpl { get; set; }

        public List<string>? BarcodeEpls { get; set; }
    }
}