using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.CourierApplicationDto;

/// <summary>Admin onay/red işlemi.</summary>
public class CourierApplicationReviewDto
{
    public int Id { get; set; }
    public bool Approve { get; set; }

    [MaxLength(500)]
    public string? RejectReason { get; set; }
}
