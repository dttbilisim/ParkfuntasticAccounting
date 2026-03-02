using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.CourierApplicationDto;

/// <summary>Mobil başvuru formu (POST /api/courier-application).</summary>
public class CourierApplicationUpsertDto
{
    [Required(ErrorMessage = "Telefon zorunludur")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(11)]
    public string? IdentityNumber { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
