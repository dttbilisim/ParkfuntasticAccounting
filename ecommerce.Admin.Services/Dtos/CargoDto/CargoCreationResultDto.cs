namespace ecommerce.Admin.Domain.Dtos.CargoDto
{
    public class CargoCreationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string CargoTrackingNumber { get; set; }
        public string CargoProvider { get; set; }
    }
}
