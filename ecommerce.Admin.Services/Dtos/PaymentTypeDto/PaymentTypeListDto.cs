namespace ecommerce.Admin.Domain.Dtos.PaymentTypeDto;

public class PaymentTypeListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsCash { get; set; }
    public bool IsCreditCard { get; set; }
    public bool IsActive { get; set; }
    public int? Type { get; set; }
    public int? CurrencyId { get; set; }
    public string? CurrencyName { get; set; }
    public bool IsPcPos { get; set; }
    public string? CompanyCode { get; set; }
}
