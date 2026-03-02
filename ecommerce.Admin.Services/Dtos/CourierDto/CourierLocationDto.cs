namespace ecommerce.Admin.Domain.Dtos.CourierDto;

public class CourierLocationDto
{
    public int Id { get; set; }
    public int CourierId { get; set; }
    public int? OrderId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime RecordedAt { get; set; }
}
