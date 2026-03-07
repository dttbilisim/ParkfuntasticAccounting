using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.PaymentTypeDto;

public class PaymentTypeUpsertDto
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    public string Name { get; set; } = null!;
    public bool IsCash { get; set; }
    public bool IsCreditCard { get; set; }
    public bool IsActive { get; set; } = true;
    public int? Type { get; set; }
    public int? CurrencyId { get; set; }
    public bool IsPcPos { get; set; }
    [MaxLength(50)]
    public string? CompanyCode { get; set; }
}
