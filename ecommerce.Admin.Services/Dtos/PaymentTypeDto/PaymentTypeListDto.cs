namespace ecommerce.Admin.Domain.Dtos.PaymentTypeDto;

public class PaymentTypeListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsCash { get; set; }
    public bool IsCreditCard { get; set; }
    public bool IsActive { get; set; }
}
