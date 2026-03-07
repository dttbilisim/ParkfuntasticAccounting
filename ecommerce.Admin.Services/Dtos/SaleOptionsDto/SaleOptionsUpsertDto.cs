using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.SaleOptionsDto;

public class SaleOptionsUpsertDto
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [MaxLength(200)]
    public string Name { get; set; } = null!;
}
