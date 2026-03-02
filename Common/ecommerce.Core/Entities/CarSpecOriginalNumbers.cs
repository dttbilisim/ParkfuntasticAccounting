namespace ecommerce.Core.Entities;
public class CarSpecOriginalNumber
{
    public int CarSpecId { get; set; }
    public CarSpec CarSpec { get; set; }

    public int OriginalNumberId { get; set; }
    public CarOriginalNumber OriginalNumber { get; set; }
}
